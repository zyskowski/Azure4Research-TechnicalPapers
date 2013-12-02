using BLAST.Web.Models;
using RecipeVVM.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Practices.Unity;
using BLAST.ProcessingUnits;
using BLAST.Web.Proxies;

namespace BLAST.Web.Controllers
{
    public class SearchTaskManagerController : ApiController
    {
        [Dependency]
        public SearchTaskPUProxy PUProxy { get; set; }

        // POST to api/testmanager to queue a new job
        public void Post(SearchTask value)
        {
            var task = RecipeVVM.Autos.EntityAdapter.Convert<SearchTask, Entities.SearchTask>(value);
            string name = task.Name;
            if (string.IsNullOrEmpty(name))
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Task name can't be null."
                });
            if (!string.IsNullOrEmpty(value.InputFiles))
            {
                var files = PUProxy.ListInputFiles();
                string[] commaParts = value.InputFiles.Split(',');
                for (int i = 0; i < commaParts.Length; i++)
                {
                    string commaPart = commaParts[i].Trim();
                    if (commaPart.IndexOf('-') >= 0)
                    {
                        string[] dashParts = commaPart.Split('-');
                        if (dashParts.Length != 2)
                            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                ReasonPhrase = "Invalid input file range."
                            });
                        int begin = 0;
                        int end = 0;
                        if (!int.TryParse(dashParts[0], out begin) || !int.TryParse(dashParts[1], out end))
                            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                ReasonPhrase = "Invalid number format."
                            });
                        for (int j = Math.Min(begin, end); j <= Math.Max(begin, end); j++)
                        {
                            string file = searchForInputFile(files, j);
                            if (string.IsNullOrEmpty(file))
                                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                                {
                                    ReasonPhrase = "Specified input file not found."
                                });
                            queueATask(task, name, file);
                        }
                    }
                    else
                    {
                        int single = 0;
                        if (!int.TryParse(commaPart, out single))
                            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                ReasonPhrase = "Invalid number format."
                            });
                        string file = searchForInputFile(files, single);
                        if (string.IsNullOrEmpty(file))
                            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                ReasonPhrase = "Specified input file not found."
                            });
                        queueATask(task, name, file);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(value.InputFile))
                queueATask(task, name, task.InputFile);
            else
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Input file not found."
                });
        }
        void queueATask(Entities.SearchTask task, string name, string file)
        {
            task.Id = "";
            task.InputFile = file;
            task.Name = name + " (" + file + ")";
            PUProxy.QueueJob(task);
        }
        string searchForInputFile(List<string> files, int index)
        {
            string pattern = "_" + index.ToString();
            foreach (string file in files)
            {
                if (file.EndsWith(pattern))
                    return file;
            }
            return "";
        }
        //GET api/testmanager to get a list of jobs
        public List<SearchTask> Get()
        {
            var list = from t in PUProxy.List()
                       orderby t.LastTimestamp descending
                       select RecipeVVM.Autos.EntityAdapter.Convert<Entities.SearchTask, SearchTask>(t);
            return list.ToList();
        }
        //DELETE api/testmanager/[id] to delete a job
        public void Delete(string id)
        {
            PUProxy.DeleteJob(id);
        }
        // PUT api/testmanager/[id] to retry a job
        public void Put(string id)
        {
            PUProxy.RetryJob(id);
        }
    }
}
