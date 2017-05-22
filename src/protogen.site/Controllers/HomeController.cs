using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text;

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
                        var parsed = FileDescriptorProto.Parse(reader, out var errors);
                        if(errors.Any())
                        {
                            var sb = new StringBuilder();
                            foreach (var error in errors)
                                sb.AppendLine(error.GetErrorMessage());
                            ViewData["error"] = sb.ToString();
                        }
                        
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
