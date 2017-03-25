using System;
using System.Web.Mvc;
using ProtoBuf.ServiceModel.Server;
using System.Web;

namespace Protobuf.Web.Mvc
{
    public abstract class ProtoController : ServerBase, IController
    {
        protected ProtoController()
        {
            
        }

        public void Execute(System.Web.Routing.RequestContext requestContext)
        {
            string action = (string)requestContext.RouteData.Values["action"];
            string service = (string)requestContext.RouteData.Values["service"];

            HttpContextBase ctx = requestContext.HttpContext;
            Execute(service, action,
                ctx.Request.Headers,
                ctx.Request.InputStream,
                ctx.Response.OutputStream,
                ctx);
        }
    }
}
