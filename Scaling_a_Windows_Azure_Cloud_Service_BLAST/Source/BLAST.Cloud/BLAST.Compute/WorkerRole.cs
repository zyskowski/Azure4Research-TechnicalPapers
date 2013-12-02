using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using BLAST.Entities;
using RecipeVVM.Base;
using RecipeVVM.Mocks;
using BLAST.ProcessingUnits;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.WindowsAzure.Storage;
using System.Runtime.Serialization.Json;
using System.IO;
using System.ServiceModel.Security;

namespace BLAST.Compute
{
    /// <summary>
    /// The WorkerRole is a host of business logic. It provides the public interface to invoke business logics, but it 
    /// doesn't implmenet any business logics by itself. In this case, the WorkerRole takes jobs via a Windows Azure 
    /// Service Bus Queue and sends the jobs to Processing Units to process.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        //Service Bus Queue variables
        const string QueueName = "JobQueue";
        QueueClient mQueueClient;
        BrokeredMessage mCurrentMessage;

        //Business logic implementation
        SearchTaskProcessingUnits mProcessingUnits;
        
        //Proxy to SignalR Hub
        HubConnection mHubConnection;
        IHubProxy  mHubProxy;
        
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        object synRoot = new object();
        private string mRoleId = "";
        public override void Run()
        {
            mQueueClient.OnMessage((receivedMessage) =>
                {
                    SearchTask task = null;
                    try
                    {
                        mCurrentMessage = receivedMessage; //Save the current message because we may need to extend the lock on it.
                        DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(BLAST.Entities.SearchTask));
                        task = receivedMessage.GetBody<SearchTask>(jsonSer);

                        //Invoke business logic
                        bool ret = mProcessingUnits.Process(task, RoleEnvironment.GetLocalResource("LocalStorage").RootPath,
                            CloudConfigurationManager.GetSetting("BOVWebSite"));

                        //Make the message as completed
                        mCurrentMessage.Complete();
                    }
                    catch (Exception ex)
                    {
                        //Here we mark the message as Complete() instead of Abadon() because we don't want to automatically retry
                        //a failed task. User needs to re-submit the job.
                        System.Diagnostics.Trace.WriteLine(
                            string.Format("Exception during processing the data. {0}, Exception:{1}",
                            receivedMessage.MessageId,
                            ex.ToString()));
                        receivedMessage.Complete();
                    }
                }, new OnMessageOptions { MaxConcurrentCalls = 1, AutoComplete = false });

            CompletedEvent.WaitOne();
        }

        public override bool OnStart()
        {

            mRoleId = RoleEnvironment.CurrentRoleInstance.Id;
            if (mRoleId.IndexOf('-') > 0)
                mRoleId = mRoleId.Substring(mRoleId.LastIndexOf('-')+1);
            else if (mRoleId.IndexOf('.') > 0)
                mRoleId = mRoleId.Substring(mRoleId.LastIndexOf('.')+1);
            string storageConnectionString = CloudConfigurationManager.GetSetting("StorageAccount");

            ServicePointManager.DefaultConnectionLimit = 12;

            //Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            //Initialize the connection to Service Bus Queue
            mQueueClient = QueueClient.CreateFromConnectionString(connectionString, QueueName);            
            
            //Set up Processing Units
            mProcessingUnits = new SearchTaskProcessingUnits(storageConnectionString, "inputncbi", "ncbi");
            mProcessingUnits.SearchTaskStatechanged += mProcessingUnits_SearchTaskStatechanged;
            
            return base.OnStart();
        }

        /// <summary>
        /// This event is triggered when the Processing Unit wants to update interested parties on progresses of jobs.
        /// </summary>
        /// <param name="sender">The Processing Unit</param>
        /// <param name="e">A SearchTaskEventArgs that carries updated states</param>
        void mProcessingUnits_SearchTaskStatechanged(object sender, SearchTaskEventArgs e)
        {
            if (e.State != "ERROR" && e.State != "OK")
            {
                if (mCurrentMessage.LockedUntilUtc - DateTime.UtcNow < TimeSpan.FromMinutes(5))
                    mCurrentMessage.RenewLock();    //Renew message lock. 
            }

            lock (synRoot)
            {
                if (mHubConnection == null)
                    initializeSignalRClient();  //Reinitialize SignalR Hub proxy.
            }

            try
            {
                //Send message to SignalR Hub.
                string messageToSend = e.Message;
                if (!string.IsNullOrEmpty(messageToSend))
                    messageToSend = "Worker Role [" + mRoleId + "]: " + messageToSend;
                mHubProxy.Invoke("Send", e.TaskId, e.State, e.Hash, e.OutputFile, messageToSend);
            }
            catch
            {
                //Clear the connection so it can be reinitialized next time.
                lock (synRoot)
                {
                    try
                    {
                        if (mHubConnection != null)
                            mHubConnection.Stop();
                    }
                    catch
                    {
                        //Failed to stop. We'll reset next
                    }
                    mHubConnection = null;
                }
            }
        }

        private  void initializeSignalRClient()
        {
            mHubConnection = new HubConnection(CloudConfigurationManager.GetSetting("HubAddress"));
            mHubProxy = mHubConnection.CreateHubProxy("MessageHub");
            mHubConnection.Start().Wait();
        }

        public override void OnStop()
        {
            mQueueClient.Close();
            CompletedEvent.Set();
            base.OnStop();
        }
    }
}
