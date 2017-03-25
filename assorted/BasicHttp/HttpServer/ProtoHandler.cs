using System;
using System.Web;
using ProtoBuf;

namespace HttpServer
{
    /// <summary>
    /// Base handler for working with .proto
    /// </summary>
    public abstract class ProtoHandler : IHttpHandler
    {
        bool IHttpHandler.IsReusable { get { return false; } }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            object request = null;
            Type requestType;
            if (context.Request.HttpMethod == "POST" && (requestType = GetRequestType(context)) != null)
            {
                request = Serializer.NonGeneric.Deserialize(requestType, context.Request.InputStream);
            }
            object response = Execute(context, request);
            if (response != null)
            {
                Serializer.NonGeneric.Serialize(context.Response.OutputStream, response);
            }
        }

        protected virtual Type GetRequestType(HttpContext context) {
            return null;
        }

        public abstract object Execute(HttpContext context, object request);


    }
}