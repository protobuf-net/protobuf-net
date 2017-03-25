using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

using System.Web.Mvc;

namespace demo_rpc_server_mvc.Controllers
{
    public static class ControllerExtensions
    {
        public static ActionResult Silverlight(this Controller controller,
            string title, string path)
        {
            if (path.IndexOf('.') < 0) path = path + ".xap";
            if(path.IndexOf('/') < 0) path = "~/ClientBin/" + path;

            var viewData = new ViewDataDictionary();
            viewData["Title"] = title;
            viewData["Path"] = path;
            
            var result = new ViewResult();
            result.ViewName = "Silverlight";
            result.ViewData = viewData;
            return result;
        }
    }
}
