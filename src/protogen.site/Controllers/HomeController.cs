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
			try
			{
				using (var reader = new StringReader(schema))
				{
					var set = new FileDescriptorSet();
					set.Add("my.proto", reader);
					var parsed = set.Files.Single();
					var errors = set.GetErrors();
					var result = new GenerateResult();
					if (errors.Length > 0)
					{
						result.ParserExceptions = errors;
					}
					result.Code = parsed.GenerateCSharp(errors: errors);
					return result;
				}
			}
			catch (Exception ex)
			{
				return new GenerateResult() { Exception = ex };
			}
		}
	}
}