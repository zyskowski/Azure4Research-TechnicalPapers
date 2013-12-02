using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BLAST.Web.Controllers
{
    public class SearchTaskController : Controller
    {
        //
        // GET: /SearchTask/

        public ActionResult Index()
        {
            ViewBag.BOVWebSiteViewAddress = CloudConfigurationManager.GetSetting("BOVWebSiteViewAddress");
            return View();
        }

        public ActionResult New()
        {
            return View();
        }
    }
}
