using System;
using System.IO;
using System.Linq;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf;

namespace protogen.site.Controllers
{

#if RELEASE
    [RequireHttps]
#endif
    public class HomeController : Controller
	{
		public IActionResult Index() => View("Index", false);

        [Route("/jsil")]
        public IActionResult ClientSide() => View("Index", true);

        [Route("/about")]
		public IActionResult About() => View();

		public IActionResult Error() => View();

		public class GenerateResult
		{
			public string Code
			{
				get;
				set;
			}

			public Error[] ParserExceptions
			{
				get;
				set;
			}

			public Exception Exception
			{
				get;
				set;
			}
		}

		[Route("/generate")]
		[HttpPost]
		public GenerateResult Generate(string schema = null)
		{
			if (string.IsNullOrWhiteSpace(schema))
			{
				return null;
			}
            var result = new GenerateResult();
            try
			{
				using (var reader = new StringReader(schema))
				{
                    var set = new FileDescriptorSet { AllowImports = false };
                    set.Add("my.proto", reader);
                    var parsed = set.Files.Single();

                    
                    set.Process();
                    var errors = set.GetErrors();
                        
                    if (errors.Length > 0)
                    {
                        result.ParserExceptions = errors;
                    }
                    result.Code = parsed.GenerateCSharp(errors: errors);
				}
			}
			catch (Exception ex)
			{
				result.Exception = ex;
			}
            return result;
		}
	}
}