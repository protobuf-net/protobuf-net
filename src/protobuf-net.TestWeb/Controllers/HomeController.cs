using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProtoBuf;
using protobuf_net.TestWeb.Models;
using System.Diagnostics;

namespace protobuf_net.TestWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Route("show")]
        public IActionResult ShowTheThing([FromBody] MyObject myObject)
        {
            // input here is via the input formatter detecting the content-type
            // header
            var tweakedObject = new MyObject
            {
                Id = myObject.Id + 42,
                Name = myObject.Name + " after magic"
            };
            // output here is via the output formatter via negotiation
            // (for a non-negotiated response, a custom ObjectResult may be needeed)
            return Ok(tweakedObject);
        }
    }

    [ProtoContract]
    public class MyObject
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }
}
