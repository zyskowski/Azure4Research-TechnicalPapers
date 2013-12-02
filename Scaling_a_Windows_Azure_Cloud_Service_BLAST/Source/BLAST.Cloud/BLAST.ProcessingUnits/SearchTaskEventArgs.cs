using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLAST.ProcessingUnits
{
    public class SearchTaskEventArgs: EventArgs
    {
        public string TaskId { get; private set; }
        public string State { get; private set; }
        public string OutputFile { get; private set; }
        public string Hash { get; private set; }
        public string Message {get;private set;}

        public SearchTaskEventArgs(string taskId, string state, string outputFile, string hash, string message)
        {
            TaskId = taskId;
            State = state;
            OutputFile = outputFile;
            Hash = hash;
            Message = message;
        }
    }
}
