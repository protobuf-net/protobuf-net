using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue295
    {
        [ProtoContract(SkipConstructor = true)]
        [ProtoInclude(500, typeof(Plant))]
        public class Asset
        {
            public Asset()
            {
                AllAssets = new List<Asset>();
                ChildAssets = new List<Asset>();
            }
            [ProtoMember(1)]
            public List<Asset> AllAssets { get; private set; }

            [ProtoMember(2)]
            public List<Asset> AssetHierarcy { get; private set; }

            [ProtoMember(3)]
            public List<Asset> ChildAssets { get; private set; }
        }
        [ProtoContract(SkipConstructor = true)]
        public class Plant : Asset
        {
            [ProtoMember(105)]
            public Asset Blowers { get; set; }
        }

        [Test]
        public void Execute()
        {
            Asset asset = new Plant {Blowers = new Asset(), ChildAssets = {new Plant()}};
            var clone = Serializer.DeepClone(asset);
        }
    }
}
