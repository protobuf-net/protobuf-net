using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProtoBuf;

namespace HttpServer
{
    public class ProtoResult : ActionResult {
        private readonly object result;
        public ProtoResult(object result) {
            this.result = result;
        }
        public override void ExecuteResult(ControllerContext context) {
            var resp = context.HttpContext.Response;
            if (result != null)
            {
                Serializer.NonGeneric.Serialize(resp.OutputStream, result);
            }
        }
    }
}