using BLAST.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using RecipeVVM.Autos;
using RecipeVVM.Base;
using RecipeVVM.Mocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BLAST.ProcessingUnits
{
    public class SearchTaskProcessingUnits : EntityProcessingUnit<string, SearchTask>
    {
        private static List<string> mInputFiles;
        CloudBlobClient mBlobClient;
        CloudBlobContainer mInputNcbiContainer;
        CloudBlobContainer mNcbiContainer;

        private object syncRoot = new object();

        public event EventHandler<SearchTaskEventArgs> SearchTaskStatechanged;

        private const string ERROR = "ERROR";
        private const string PENDING = "PENDING";
        private const string OK = "OK";

        public SearchTaskProcessingUnits(string connectionString, string inputncbi, string ncbi)
            : base(new RecipeVVM.WindowsAzure.Storage.TableStoreageEntityRepo<SearchTask>("Id","Id", connectionString, "SearchTask"))
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            mBlobClient = storageAccount.CreateCloudBlobClient();
            mInputNcbiContainer = mBlobClient.GetContainerReference(inputncbi);
            mNcbiContainer = mBlobClient.GetContainerReference(ncbi);
        }
        public List<string> ListInputFiles()
        {
            if (mInputFiles == null)
            {
                lock (syncRoot)
                {
                    if (mInputFiles == null)
                    {
                        mInputFiles = new List<string>();
                        foreach (var item in mInputNcbiContainer.ListBlobs(null, true))
                        {
                            mInputFiles.Add(((CloudBlockBlob)item).Name);
                        }
                    }
                }
            }
            return mInputFiles;
        }

        private void raiseEvent(string taskId, string state, string message)
        {
            raiseEvent(taskId, state, "","", message);
        }
        private void raiseEvent(SearchTask task, string state, string message)
        {
            raiseEvent(task.Id, state, task.OutputFile, task.Hash, message);
        }
        private void raiseEvent(string taskId, string state, string outputFile, string hash, string message)
        {
            if (!string.IsNullOrEmpty(taskId))
            {
                try
                {
                    var task = Read(taskId, partitionId: taskId);
                    task.Hash = hash;
                    task.State = state;
                    task.LastMessage = message;
                    task.OutputFile = outputFile;
                    Update(task);
                }
                catch
                {
                    //Doesn't matter if save failed.
                }
            }

            if (SearchTaskStatechanged != null)
                SearchTaskStatechanged(this, new SearchTaskEventArgs(taskId, state, outputFile, hash, message));
        }

        public bool Process(SearchTask task, string root, string BOVWebSite)
        {
            try
            {
                raiseEvent(task, PENDING, "Running...");

                string inputPath = Path.Combine(root, "inputncbi");
                string dbPath = Path.Combine(root, "ncbi");
                string outFile = Guid.NewGuid().ToString("N") + ".txt";
                string outPath = Path.Combine(root, outFile);

                //1. Get NCBI database files from BLOB if necessary
                populateDatabase(task.Id, dbPath);

                //2. Get Input file if it hasn't been cached locally
                if (!getInputFile(task.Id, inputPath, task.InputFile))
                    raiseEvent(task, ERROR, "Failed to download input file: " + task.InputFile);

                //3. Launch bastn.exe to search
                raiseEvent(task, PENDING, "Searching...");
                ProcessStartInfo info = new ProcessStartInfo("blastn.exe",
                    string.Format("-db \"{0}\\est_human\" -query \"{1}\\{2}\" -out \"{3}\"",
                    dbPath, inputPath, task.InputFile, outPath));
                info.CreateNoWindow = true;
                var process = System.Diagnostics.Process.Start(info);
                try
                {
                    process.WaitForExit();
                }
                catch (Exception exp)
                {
                    raiseEvent(task, ERROR, "Failed to execute blastn.exe: " + exp.Message);
                    return false;
                }
                raiseEvent(task, PENDING, "Search Completed.");

                //4. Upload result file to BOV Web Site
                if (File.Exists(outPath))
                {
                    string hash = null;
                    string ret = uploadFileToBOVWebSite(outPath, BOVWebSite, out hash);
                    if (!string.IsNullOrEmpty(ret))
                    {
                        raiseEvent(task, ERROR, "Failed to upload result to BOV Web Site: " + ret);
                        return false;
                    }
                    else
                    {
                        task.OutputFile = outFile;
                        task.Hash = hash;
                        task.State = OK;
                        task.LastMessage = "Completed";
                        Update(task);
                        raiseEvent(task, OK, "Completed");
                        return true;
                    }
                }
                else
                {
                    raiseEvent(task, ERROR, "Failed to execute blastn.exe. Output file not generated.");
                    return false;
                }
            }
            catch (Exception exp)
            {
                raiseEvent(task, ERROR, "Failed to process task: " + exp.Message);
                return false;
            }
        }

        private static HttpWebResponse UploadAndGetResponse(string file, string BOVWebSite)
        {
            // Read file data        
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();

            // Generate post objects     
            Dictionary<string, object> postParameters = new Dictionary<string, object>();
            postParameters.Add("MAX_FILE_SIZE", "1000000");
            postParameters.Add("email", "");
            postParameters.Add("uploadfile", data);

            // Create request and receive response     
            string postURL = BOVWebSite;
            string userAgent = "---";
            HttpWebResponse webResponse = WebHelpers.MultipartFormDataPost(postURL, userAgent, postParameters);
            return webResponse;
        }
        private string uploadFileToBOVWebSite(string file, string BOVWebSite, out string hash)
        {
            HttpWebResponse response = null;
            hash = null;
            try
            {
                response = UploadAndGetResponse(file, BOVWebSite);
            }
            catch (Exception ex)
            {
               return "Error while uploading file: " + ex.Message;
            }

            string query = response.ResponseUri.Query;
            if (query.IndexOf('=') < 0)
            {
                return "Error in response";
            }
            else
            {
                hash = query.Substring(query.IndexOf('=') + 1);
            }
            response.Close();
            return "";
        }
        private bool getInputFile(string taskId, string inputRoot, string file)
        {
            if (!Directory.Exists(inputRoot))
                Directory.CreateDirectory(inputRoot);
            string inputPath = Path.Combine(inputRoot, file);
            if (File.Exists(inputPath))
                return true;
            var blob = mInputNcbiContainer.GetBlockBlobReference(file);
            return downloadBlob(taskId, blob, inputPath);
        }
        private bool downloadBlob(string taskId, CloudBlockBlob blob, string filepath)
        {
            blob.FetchAttributes();
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using (Stream blobStream = blob.OpenRead())
                    {
                        using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
                        {
                            byte[] buffer = new byte[200960];
                            int read;
                            long count = 0;
                            while ((read = blobStream.Read(buffer, 0, 200960)) > 0)
                            {
                                fileStream.Write(buffer, 0, read);
                                count += read;
                                double percent = blob.Properties.Length;
                                if (percent > 0)
                                 percent = count / percent;
                                else
                                    percent = 0;
                                raiseEvent(taskId, PENDING, string.Format("Downloading {0} ({1:P})", blob.Name, percent));
                            }
                        }
                    }
                    return true;
                }
                catch
                {
                    if (File.Exists(filepath))
                        File.Delete(filepath);
                    raiseEvent(taskId, PENDING,  "Download " + blob.Name + " failed. Retrying...");
                    Thread.Sleep(3000);
                }
            }
            return false;
        }
        private void populateDatabase(string taskId, string ncbiPath)
        {
            if (!Directory.Exists(ncbiPath))
                Directory.CreateDirectory(ncbiPath);
            string[] files = Directory.GetFiles(ncbiPath);
            if (files.Length >= 17) //Shortcut to exit quicker
                return;
            foreach (var item in mNcbiContainer.ListBlobs(null, true))
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                string filepath = Path.Combine(ncbiPath, blob.Name);
                if (!File.Exists(filepath))
                    downloadBlob(taskId, blob, filepath);
            }
        }
    }
}
