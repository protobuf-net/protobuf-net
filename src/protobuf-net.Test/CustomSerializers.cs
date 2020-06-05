using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using ProtoBuf.unittest;
using System;
using Xunit;

namespace ProtoBuf
{
    public class CustomSerializers
    {
        [Fact]
        public void CustomSerializerDetectedOnModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<HazCustomSerializer>();
            model.Add<HazIndirectCustomSerializer>();
            var rt = model[typeof(HazCustomSerializer)];
            Assert.Equal(typeof(CustomSerializer), rt.SerializerType);

            // detected on runtime model
            Test(model);

            // detected on runtime model (compiled)
            model.CompileInPlace();
            Test(model);

            // detected on fully compiled model (and check IL)
            var compiled = model.CompileAndVerify();
            Test(compiled);

            // detected on in-process compiled model 
            compiled = model.Compile();
            Test(compiled);

            static void Test(TypeModel model)
            {
                object obj = model.GetSerializerCore<HazCustomSerializer>(default);
                Assert.IsType<CustomSerializer>(obj);

                obj = model.GetSerializerCore<HazIndirectCustomSerializer>(default);
                Assert.IsType<IndirectSerializer>(obj);
            }
        }

        [Fact]
        public void CustomSerializerWorks()
        {
            var obj = new HazCustomSerializer { Inner = { Name = nameof(CustomSerializerWorks) } };
            Assert.Equal(CustomSerializerOperations.None, obj.ViaCustomSerializer);

            var clone = Serializer.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(CustomSerializerOperations.Write, obj.ViaCustomSerializer);
            Assert.Equal(CustomSerializerOperations.Read, clone.ViaCustomSerializer);
            Assert.Equal(nameof(CustomSerializerWorks), clone.Inner.Name);
        }

        [Flags]
        internal enum CustomSerializerOperations
        {
            None = 0, Read = 1, Write = 2
        }

        [ProtoContract(Serializer = typeof(CustomSerializer))]
        public class HazCustomSerializer
        {
            public HazIndirectCustomSerializer Inner { get; } = new HazIndirectCustomSerializer();

            internal CustomSerializerOperations ViaCustomSerializer { get; set; }
        }

        [ProtoContract(Serializer = typeof(CustomSerializer))]
        public class HazIndirectCustomSerializer
        {
            public string Name { get; set; } 

            internal CustomSerializerOperations ViaCustomSerializer { get; set; }
        }

        public class CustomSerializer : ISerializer<HazCustomSerializer>, ISerializerProxy<HazIndirectCustomSerializer>
        {
            SerializerFeatures ISerializer<HazCustomSerializer>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

            ISerializer<HazIndirectCustomSerializer> ISerializerProxy<HazIndirectCustomSerializer>.Serializer
                => SerializerCache.Get<IndirectSerializer, HazIndirectCustomSerializer>();

            void ISerializer<HazCustomSerializer>.Write(ref ProtoWriter.State state, HazCustomSerializer value)
            {
                state.WriteMessage(1, default, value.Inner);
                value.ViaCustomSerializer |= CustomSerializerOperations.Write;
            }

            HazCustomSerializer ISerializer<HazCustomSerializer>.Read(ref ProtoReader.State state, HazCustomSerializer value)
            {
                int field;
                value ??= new HazCustomSerializer();
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            state.ReadMessage(default, value.Inner);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                value.ViaCustomSerializer |= CustomSerializerOperations.Read;
                return value;
            }
        }
        public class IndirectSerializer : ISerializer<HazIndirectCustomSerializer>
        {
            SerializerFeatures ISerializer<HazIndirectCustomSerializer>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

            HazIndirectCustomSerializer ISerializer<HazIndirectCustomSerializer>.Read(ref ProtoReader.State state, HazIndirectCustomSerializer value)
            {
                int field;
                value ??= new HazIndirectCustomSerializer();
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch(field)
                    {
                        case 1:
                            value.Name = state.ReadString();
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                value.ViaCustomSerializer |= CustomSerializerOperations.Read;
                return value;
            }

            void ISerializer<HazIndirectCustomSerializer>.Write(ref ProtoWriter.State state, HazIndirectCustomSerializer value)
            {
                state.WriteString(1, value.Name);
                value.ViaCustomSerializer |= CustomSerializerOperations.Write;
            }
        }
    }
}
