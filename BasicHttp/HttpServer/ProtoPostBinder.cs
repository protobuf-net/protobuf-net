using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProtoBuf;

namespace HttpServer
{
    public class ProtoPostBinder : IModelBinder {
        object IModelBinder.BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
            var req = controllerContext.HttpContext.Request;
            if(req.HttpMethod == "POST") {
                return Serializer.NonGeneric.Deserialize(bindingContext.ModelType, req.InputStream);
            }
            return null;
        }
    }

    public class ProtoPostAttribute : CustomModelBinderAttribute
    {
        public override IModelBinder GetBinder()
        {
            return new ProtoPostBinder();
        }
    }
}