using System;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf;

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
						var set = new FileDescriptorSet();
						set.Add("my.proto", reader);
						var parsed = set.Files.Single();
						var errors = set.GetErrors();
						if (errors.Any())
						{
							var sb = new StringBuilder();
							foreach (var error in errors)
								sb.AppendLine(error.ToString());
							ViewData["error"] = sb.ToString();
						}

						ViewData["code"] = parsed.GenerateCSharp(errors: errors);
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
					if (errors.Length > 0)
					{
						return new GenerateResult() { ParserExceptions = errors };
					}

					return new GenerateResult() { Code = parsed.GenerateCSharp(errors: errors) };
				}
			}
			catch (Exception ex)
			{
				return new GenerateResult() { Exception = ex };
			}
		}
	}
}