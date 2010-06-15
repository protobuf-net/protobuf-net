using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyDtoLayer;

namespace HttpServer.Controllers
{
    public class CustomerController : Controller
    {
        public ActionResult GetCustomer([ProtoPost] GetCustomerRequest req)
        {
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
            return new ProtoResult(resp);
        }

    }
}
