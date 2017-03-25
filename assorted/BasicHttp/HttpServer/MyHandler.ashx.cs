using System;
using System.Web;
using MyDtoLayer;

namespace HttpServer
{
    public class MyHandler : ProtoHandler
    {
        public override object Execute(HttpContext context, object request) {
            // inspect the incoming request
            var req = (GetCustomerRequest)request;

            // create a response
            var resp = new GetCustomerResponse {
                cust = new Customer {
                    id = req.id,
                    name = "Name of cust " + req.id,
                    address = new Address {
                        line1 = "27 wood lane", zip = "pl1"
                    }
                }
            };
            return resp;
        }
        protected override Type GetRequestType(HttpContext context)
        {
            // tell the base what type of request we should expect (this could vary with context)
            return typeof(GetCustomerRequest);
        }
    }
}