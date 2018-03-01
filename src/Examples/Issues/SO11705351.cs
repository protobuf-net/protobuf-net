using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace Examples.Issues
{
    // note that some additional changes were needed beyond what is shown on SO
    // in order to fully test standalone compilation / PEVerify; mainly due to
    // public readonly fields, which protobuf-net will still try and mutate
     
    public class SO11705351
    {
        [ProtoContract]
        public class Whole
        {
            private readonly PartCollection parts;

            public Whole() { parts = new PartCollection { Whole = this }; }
            [ProtoMember(1)]
            public PartCollection Parts { get { return parts; } }
        }

        [ProtoContract]
        public class Part
        {
            [ProtoMember(1, AsReference = true)]
            public Whole Whole { get; set; }
        }

        [ProtoContract(IgnoreListHandling = true)]
        public class PartCollection : List<Part>
        {
            public Whole Whole { get; set; }
        }

        [ProtoContract]
        public class Assemblage
        {
            private readonly PartCollection parts = new PartCollection();
            [ProtoMember(1)]
            public PartCollection Parts { get { return parts; }}
        }

        [ProtoContract]
        public class PartCollectionSurrogate
        {
            [ProtoMember(1, AsReference = true)]
            public List<Part> Collection { get; set; }

            [ProtoMember(2, AsReference = true)]
            public Whole Whole { get; set; }

            public static implicit operator PartCollectionSurrogate(PartCollection value)
            {
                if (value == null) return null;
                return new PartCollectionSurrogate { Collection = value, Whole = value.Whole };
            }

            public static implicit operator PartCollection(PartCollectionSurrogate value)
            {
                if (value == null) return null;

                PartCollection result = new PartCollection {Whole = value.Whole};
                if(value.Collection != null)
                { // add the data we colated
                    result.AddRange(value.Collection);
                }
                return result;
            }
        }

        static RuntimeTypeModel GetModel()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(PartCollection), true).SetSurrogate(typeof(PartCollectionSurrogate));
            return model;
        }
        static Assemblage GetData()
        {
            var whole = new Whole();
            var part = new Part { Whole = whole };
            whole.Parts.Add(part);
            var assemblage = new Assemblage();
            assemblage.Parts.Add(part);
            return assemblage;
        }
        [Fact]
        public void Execute()
        {
            var model = GetModel();
            ExecuteImpl(model, "Runtime");
            model.CompileInPlace();
            ExecuteImpl(model, "CompileInPlace");
            ExecuteImpl(model.Compile(), "Compile");
            model.Compile("SO11705351", "SO11705351.dll");
            PEVerify.AssertValid("SO11705351.dll");
        }
        private static void ExecuteImpl(TypeModel model, string caption)
        {
            
            using (var stream = new MemoryStream())
            {
                {
                    var assemblage = GetData();
                    model.Serialize(stream, assemblage);
                }

                stream.Position = 0;

                var obj = (Assemblage) model.Deserialize(stream, null, typeof (Assemblage));
                {
                    var assemblage = obj;
                    var whole = assemblage.Parts[0].Whole;

                    Assert.Same(assemblage.Parts[0].Whole, whole.Parts[0].Whole); //, "Whole:" + caption);
                    Assert.Same(assemblage.Parts[0], whole.Parts[0]); //, "Part:" + caption);
                }
            }
        }

        [Fact]
        public void CheckSchema()
        {
            var model = GetModel();
            model.Serialize(Stream.Null, GetData()); // to bring the other types into play

            string schema = model.GetSchema(null);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""protobuf-net/protogen.proto""; // custom protobuf-net options

message Assemblage {
   optional PartCollectionSurrogate Parts = 1;
}
message Part {
   optional .bcl.NetObjectProxy Whole = 1 [(.protobuf_net.fieldopt).asRef = true]; // reference-tracked Whole
}
message PartCollectionSurrogate {
   repeated .bcl.NetObjectProxy Collection = 1 [(.protobuf_net.fieldopt).asRef = true]; // reference-tracked Part
   optional .bcl.NetObjectProxy Whole = 2 [(.protobuf_net.fieldopt).asRef = true]; // reference-tracked Whole
}
message Whole {
   optional PartCollectionSurrogate Parts = 1;
}
", schema);
        }
    }
}
