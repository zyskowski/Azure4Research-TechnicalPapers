using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BLAST.Entities
{
    /// <summary>
    /// SearchTask is the only entity used in this system. This is a simplied design. Ideally,
    /// temporal states, such as State and LastMessage, should be detached from the main entity.
    /// </summary>
    [DataContract]
    public class SearchTask
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string InputFile { get; set; }
        [DataMember]
        public string Hash { get; set; }
        [DataMember]
        public string OutputFile { get; set; }
        [DataMember]
        public string State { get; set; }
        [DataMember]
        public string LastMessage { get; set; }
        [DataMember]
        public string InputFiles { get; set; }
        [DataMember]
        public long LastTimestamp { get; set; }
    }
}
