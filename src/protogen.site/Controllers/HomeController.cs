using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace protogen.site.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string schema = null)
        {
            if (!string.IsNullOrWhiteSpace(schema))
            {
                ViewData["schema"] = schema;
                try
                {
                    using (var reader = new StringReader(schema))
                    {
                        var parsed = FileDescriptorProto.Parse(reader);
                        ViewData["code"] = parsed.GenerateCSharp();
                    }
                }
                catch (Exception ex)
                {
                    ViewData["error"] = ex.Message;
                }
            }
            return View();
        }

        [Route("/about")]
        public IActionResult About() => View();

        public IActionResult Error() => View();
    }
}
