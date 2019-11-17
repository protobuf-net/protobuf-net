#if !COREFX
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DAL;
using ProtoBuf.Meta;
using Xunit;
using Xunit.Abstractions;

namespace Examples.Issues
{
    public class Issue176
    {
        private ITestOutputHelper Log { get; }
        public Issue176(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void TestOrderLineGetDeserializedAndAttachedToOrder()
        {

            byte[] fileBytes = File.ReadAllBytes(NWindTests.GetNWindBinPath());

            RuntimeTypeModel ordersModel = RuntimeTypeModel.Create();
            ordersModel.AutoCompile = false;

#pragma warning disable CS0618
            Database database = (Database)ordersModel.Deserialize(new MemoryStream(fileBytes), null, typeof(Database));
#pragma warning restore CS0618
            List<Order> orders = database.Orders;

            DbMetrics("From File", orders);

            var roundTrippedOrders = (List<Order>)ordersModel.DeepClone(orders);
            Assert.NotSame(orders, roundTrippedOrders);
            DbMetrics("Round trip", roundTrippedOrders);
            Assert.Equal(orders.SelectMany(o => o.Lines).Count(),
                roundTrippedOrders.SelectMany(o => o.Lines).Count()); //, "total count");
        }

        private void DbMetrics(string caption, IEnumerable<Order> orders)
        {
            int count = orders.Count();
            int lines = orders.SelectMany(ord => ord.Lines).Count();
            int totalQty = orders.SelectMany(ord => ord.Lines)
                    .Sum(line => line.Quantity);
            decimal totalValue = orders.SelectMany(ord => ord.Lines)
                    .Sum(line => line.Quantity * line.UnitPrice);

            Log.WriteLine("{0}\torders {1}; lines {2}; units {3}; value {4:C}",
                              caption, count, lines, totalQty, totalValue);
        }

    }
}
#endif