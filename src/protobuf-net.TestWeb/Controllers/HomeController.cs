using Microsoft.AspNetCore.Mvc;
using ProtoBuf;

namespace protobuf_net.TestWeb.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Home() => Ok("server is running");

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
