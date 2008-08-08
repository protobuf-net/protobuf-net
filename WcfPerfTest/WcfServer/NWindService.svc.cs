using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data.Linq;


namespace WcfServer
{

    public sealed class NWindMtomService : NWindService { }
    public sealed class NWindTextService : NWindService { }

    public abstract class NWindService : INWindService
    {

        static OrderSet SharedLoad()
        {
            OrderSet result = new OrderSet();
            using (var ctx = new NWindDataContext())
            {
                DataLoadOptions options = new DataLoadOptions();
                options.LoadWith<Order>(order => order.Order_Details);
                ctx.LoadOptions = options;
                result.Orders.AddRange(ctx.Orders);
            }
            return result;
        }
        public OrderSet LoadFoo()
        {
            return SharedLoad();
        }
        public OrderSet LoadBar()
        {
            return SharedLoad();
        }

        public OrderSet RoundTripFoo(OrderSet set)
        {
            return set;
        }

        public OrderSet RoundTripBar(OrderSet set)
        {
            return set;
        }
    }

    [DataContract]
    public class OrderSet
    {
        public OrderSet() { Orders = new List<Order>(); }

        [DataMember(Order = 1)]
        public List<Order> Orders { get; private set; }
    }

}
