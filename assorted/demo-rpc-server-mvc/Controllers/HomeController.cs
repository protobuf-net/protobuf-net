using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace demo_rpc_server_mvc.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        public ActionResult DemoApp()
        {
            return this.Silverlight("Demo App", "demo-rpc-client-silverlight");
        }

        public ActionResult Index()
        {
            ViewData["Message"] = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
