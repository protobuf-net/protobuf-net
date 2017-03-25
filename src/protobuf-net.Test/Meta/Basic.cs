using System.IO;
using System.Linq;
using NUnit.Framework;
using ProtoBuf.Meta;
using ProtoBuf.unittest.Serializers;

namespace ProtoBuf.unittest.Meta
{

    [TestFixture]
    public class Basic
    {
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public static RuntimeTypeModel BuildMeta()
            {
                var model = TypeModel.Create();
                model.Add(typeof(Customer), false)
                    .Add(1, "Id")
                    .Add(2, "Name");
                return model;
            }
        }

        
        [Test]
        public void CanInitializeExplicitMeta()
        {
            var meta = Customer.BuildMeta();
            Assert.IsNotNull(meta);
            var types = meta.GetTypes().Cast<MetaType>();
            Assert.AreEqual(typeof(Customer), types.Single().Type);
        }

        [Test]
        public void WriteBasicRuntime()
        {
            var meta = Customer.BuildMeta();
            Customer cust = new Customer { Id = 123, Name = "abc"};

            // Id: 1 * 8 + 0 = 0x08
            // 123: 0x7B
            // Name: 2 * 8 + 2 = 0x12
            // "abc": 0x03616263

            Util.TestModel(meta, cust, "087B1203616263");
        }

        [Test]
        public void WriteRoundTripRuntime()
        {
            var meta = Customer.BuildMeta();
            Customer cust = new Customer { Id = 123, Name = "abc" };

            using (var ms = new MemoryStream())
            {
                meta.Serialize(ms, cust);
                Assert.Greater(1, 0, "check arg order API");
                Assert.Greater(ms.Length, 0, "no data written");
                ms.Position = 0;
                Customer clone = (Customer)meta.Deserialize(ms, null, typeof(Customer));
                Assert.AreNotSame(cust, clone);
                Assert.IsNotNull(clone, "clone was not materialized");
                Assert.AreEqual(cust.Id, clone.Id);
                Assert.AreEqual(cust.Name, clone.Name);
            }
        }
        
    }

}
