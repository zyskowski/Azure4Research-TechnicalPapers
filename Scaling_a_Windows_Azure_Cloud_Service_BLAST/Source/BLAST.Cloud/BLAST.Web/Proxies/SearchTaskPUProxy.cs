using BLAST.ProcessingUnits;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using RecipeVVM.Base;
using RecipeVVM.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Web;

namespace BLAST.Web.Proxies
{
    /// <summary>
    /// The Proxy decouples frontend from backend business logics. 
    /// </summary>
    public class SearchTaskPUProxy
    {
        private QueueClient mTaskQueue;
        private SearchTaskProcessingUnits mProcessingUnit;
        private const string QueueName = "JobQueue";

        public SearchTaskPUProxy()
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(QueueName))
                namespaceManager.CreateQueue(QueueName);
            mTaskQueue = QueueClient.CreateFromConnectionString(connectionString, QueueName);
            mProcessingUnit = new SearchTaskProcessingUnits(CloudConfigurationManager.GetSetting("StorageAccount"), "inputncbi", "ncbi");
        }

        public void QueueJob(Entities.SearchTask task)
        {
            if (string.IsNullOrEmpty(task.Id))
            {
                task.Id = Guid.NewGuid().ToString("N");
            }
            task.LastTimestamp = DateTime.UtcNow.Ticks;
            task.State = "QUEUED";
            task.LastMessage = "Queued";
            DataContractJsonSerializer jsonSer = new DataContractJsonSerializer (typeof(BLAST.Entities.SearchTask));
            BrokeredMessage msg = new BrokeredMessage(task, jsonSer);
            mTaskQueue.Send(msg);
            mProcessingUnit.Create(task);
        }
        
        public void DeleteJob(string id)
        {
            if (id == "OK" || id == "QUEUED")
            {
                var tasks = mProcessingUnit.List();
                foreach (var task in tasks)
                {
                    try
                    {
                        if (task.State == id || (id=="QUEUED" && task.State == ""))
                            mProcessingUnit.Delete(task.Id, partitionId: task.Id);
                    }
                    catch
                    {
                    }
                }
            }
            else 
                mProcessingUnit.Delete(id, partitionId:id);
        }
        
        public void RetryJob(string id)
        {
            var task = mProcessingUnit.Read(id, partitionId: id);
            if (task != null)
            {
                task.LastTimestamp = DateTime.UtcNow.Ticks;
                mTaskQueue.Send(new BrokeredMessage(task));
            }
        }
        
        public IEnumerable<Entities.SearchTask> List()
        {
            try
            {
                return mProcessingUnit.List();
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public List<String> ListInputFiles()
        {
            return mProcessingUnit.ListInputFiles();
        }
    }
}