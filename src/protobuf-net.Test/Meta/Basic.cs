using System.IO;
using System.Linq;
using Xunit;
using ProtoBuf.Meta;
using ProtoBuf.unittest.Serializers;

namespace ProtoBuf.unittest.Meta
{

    public class DefaultModel
    {
        [Fact]
        public void DefaultModelAvailable()
        {
            TypeModel.ResetDefaultModel();
            var model = TypeModel.DefaultModel;
            Assert.IsType<TypeModel.NullModel>(model);

            _ = RuntimeTypeModel.Default;
            model = TypeModel.DefaultModel;
            Assert.IsType<RuntimeTypeModel>(model);
        }
    }
    public class Basic
    {
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public static RuntimeTypeModel BuildMeta()
            {
                var model = RuntimeTypeModel.Create();
                model.Add(typeof(Customer), false)
                    .Add(1, "Id")
                    .Add(2, "Name");
                return model;
            }
        }

        
        [Fact]
        public void CanInitializeExplicitMeta()
        {
            var meta = Customer.BuildMeta();
            Assert.NotNull(meta);
            var types = meta.GetTypes().Cast<MetaType>();
            Assert.Equal(typeof(Customer), types.Single().Type);
        }

        [Fact]
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

        [Fact]
        public void WriteRoundTripRuntime()
        {
            var meta = Customer.BuildMeta();
            Customer cust = new Customer { Id = 123, Name = "abc" };

            using var ms = new MemoryStream();
            meta.Serialize(ms, cust);
            Assert.NotEqual(0, ms.Length); //, "no data written");
            ms.Position = 0;
#pragma warning disable CS0618
            Customer clone = (Customer)meta.Deserialize(ms, null, typeof(Customer));
#pragma warning restore CS0618
            Assert.NotSame(cust, clone);
            Assert.NotNull(clone); //, "clone was not materialized");
            Assert.Equal(cust.Id, clone.Id);
            Assert.Equal(cust.Name, clone.Name);
        }
        
    }

}
