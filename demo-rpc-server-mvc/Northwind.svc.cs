using System.ServiceModel;
using System.Linq;
using System.ServiceModel.Activation;
using demo_rpc_server_mvc.Models;

namespace demo_rpc_server_mvc
{
    [ServiceContract(Namespace = "")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class Northwind : INorthwind
    {
        [OperationContract]
        public Customer[] GetCustomers()
        {
            using (var ctx = CreateContext())
            {
                return ctx.Customers.ToArray();
            }
        }

        NorthwindDataContext CreateContext()
        {
            return new NorthwindDataContext();
        }

        [OperationContract]
        public Order[] GetOrders(string customerId)
        {
            using (var ctx = CreateContext())
            {
                return ctx.Orders
                        .Where(order => order.CustomerID == customerId)
                        .ToArray();
            }
        }
    }
}
