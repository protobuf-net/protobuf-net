using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WcfServer
{
    /// <summary>
    /// Summary description for BasicService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class BasicService1 : System.Web.Services.WebService
    {

        [WebMethod]
        public BasicType BasicOperation()
        {
            return new BasicType { Id = 123, Name = "abc" };
        }
    }
}
