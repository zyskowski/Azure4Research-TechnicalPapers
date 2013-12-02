using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLAST.Web
{
    public class MessageHub: Hub
    {
        public void Send(string taskId, string state, string hash, string outputFile, string message)
        {
            Clients.All.broadcastMessage(taskId, state, hash, outputFile,message);
        }
    }
}