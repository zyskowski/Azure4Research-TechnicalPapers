using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLAST.Web.Models
{
    public class SearchTask
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string InputFile { get; set; }
        public string Hash { get; set; }
        public string OutputFile { get; set; }
        public string State { get; set; }
        public string LastMessage {get;set;}
        public string InputFiles { get; set; }
        public long LastTimestamp { get; set; }
        public SearchTask()
        {
            Id = "";
            Name = "";
            InputFile = "";
            Hash = "";
            OutputFile = "";
            State = "";
            LastMessage = "";
            InputFiles = "";
            LastTimestamp = DateTime.UtcNow.Ticks;
        }
    }
}