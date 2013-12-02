using BLAST.Web.Proxies;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BLAST.Web.Controllers
{
    public class InputFileManagerController : ApiController
    {
        [Dependency]
        public SearchTaskPUProxy PUProxy { get; set; }

        public List<string> Get()
        {
            return PUProxy.ListInputFiles();
        }
    }
}
