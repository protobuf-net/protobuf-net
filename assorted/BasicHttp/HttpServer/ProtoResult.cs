using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProtoBuf;

namespace HttpServer
{
    public class ProtoResult : ActionResult
    {
        // somewhere to store the value the controller gives us
        private readonly object result;
        public ProtoResult(object result) { this.result = result; }

        // write the response
        public override void ExecuteResult(ControllerContext context)
        {
            var resp = context.HttpContext.Response;
            if (result != null)
            {
                Serializer.NonGeneric.Serialize(resp.OutputStream, result);
            }
        }
    }
}