using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Examples
{
    public class ImmutablePartial
    {
        //This example shows the following techniques:
        //
        // - how to serialise an immutable model using Surrogates
        // - how to omit 'leaf' nodes from the object graph from the serialized
        //   string and instead store an identifier
        // - how to restore omitted 'leaf' nodes during deserialization from a
        //   dependency-injected repository
        // - how to avoid Proto-attributes on domain models 
   
        /// <summary>
        /// Kind is the leaf node - when serializing a Kind we only wish to store
        /// the Id in the stream
        /// </summary>
        public class Kind
        {
            public Kind(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }
            public string Name { get; }
        }

        /// <summary>
        /// This is the interface for the repository from which a Kind can be
        /// retrieved by Id during deserialization
        /// </summary>
        public interface IKindRepository
        {
            Kind Get(int id);
        }

        /// <summary>
        /// A sample implementation of the respostiory
        /// </summary>
        public class KindRepository : IKindRepository
        {
            public Kind Get(int id) => new Kind(id, "Kind" + id.ToString());
        }


        /// <summary>
        /// Item is the immutable sample domain model we wish to serialize
        /// </summary>
        public class Item
        {
            /// <summary>
            /// An ordinary property
            /// </summary>
            public string Name { get; }
            /// <summary>
            /// The Kind property, which we don't want to serialize in full
            /// </summary>
            public Kind Kind { get; }


            public Item(string name, Kind kind)
            {
                Name = name;
                Kind = kind;
            }
        }


        /// <summary>
        /// To serialize the model we use surrogate
        /// </summary>
        [ProtoContract]
        public class ItemSurrogate
        {
            /// <summary>
            /// Name is serialized normally
            /// </summary>
            [ProtoMember(1)]
            public string Name { get; set; }

            /// <summary>
            /// Kind is serialized as a nullable integer Id
            /// </summary>
            [ProtoMember(2)]
            public int? KindId { get; set; }

            /// <summary>
            /// The use of Covert methods is a new feature of Surrogates. It can be used in place of the implicit conversion operators,
            /// and it provides access to the SerializationContent to allow access to the dependency-injected repository to take place.
            /// 
            /// This convertor uses the repostiory to retireve the Kind with the specified Id
            /// </summary>
            /// <param name="s">The surrogate being de-serialized</param>
            /// <param name="context">The <see cref="SerializationContext"/></param>
            /// <returns></returns>
            public static Item Convert(ItemSurrogate s, SerializationContext context)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? ((IKindRepository)context.Context).Get(s.KindId.Value) : null);
            }

            /// <summary>
            /// The SerializationContext argument to the Convert method is optional, and implict operators are still supported.
            /// This converter retrieves the Id of the Kind being serialized
            /// </summary>
            /// <param name="o"></param>
            /// <returns></returns>
            public static ItemSurrogate Convert(Item o)
            {
                return o == null ? null : new ItemSurrogate { Name = o.Name, KindId = o.Kind?.Id };
            }

        }

        /// <summary>
        /// This is our serializer for Items. In a real-world scenario, this would implement an IItemSerializer interface
        /// and not need the compile or surrogate constructor arguments.
        /// </summary>
        public class ItemSerializer
        {
            private readonly IKindRepository kindRepository;
            private TypeModel typeModel;

            /// <summary>
            /// Create a serializer
            /// </summary>
            /// <param name="kindRepository">Dependency-injected repository to deserialize Kind objects</param>
            /// <param name="compile">True if this unit test should compile the model</param>
            /// <param name="surrogate">Surrogate to test with</param>
            public ItemSerializer(IKindRepository kindRepository, bool compile, Type surrogate)
            {
                this.kindRepository = kindRepository;
                var model = RuntimeTypeModel.Create();
                var item = model.Add(typeof(Item), false);
                item.SetSurrogate(surrogate);
                typeModel = compile ? model.Compile() : model;
            }

            public void Serialize(Stream s, Item i)
            {
                typeModel.Serialize(s, i);
            }

            public Item Deserialize(Stream s)
            {
                return (Item)typeModel.Deserialize(s, null, typeof(Item), new SerializationContext
                {
                    //Pass the kindRepository as the custom Context property so it can be retireved
                    //in the Convert method of the ItemSurrogate
                    Context = kindRepository
                });
            }
        }

        [Theory]
        [InlineData(true,typeof(ItemSurrogate))]
        [InlineData(false, typeof(ItemSurrogate))]
        [InlineData(true, typeof(ItemSurrogate2))]
        [InlineData(false, typeof(ItemSurrogate2))]
        [InlineData(true, typeof(ItemSurrogate3))]
        [InlineData(false, typeof(ItemSurrogate3))]
        [InlineData(true, typeof(ItemSurrogate4))]
        [InlineData(false, typeof(ItemSurrogate4))]
        [InlineData(true, typeof(ItemSurrogate5))]
        [InlineData(false, typeof(ItemSurrogate5))]
        [InlineData(true, typeof(ItemSurrogate6))]
        [InlineData(false, typeof(ItemSurrogate6))]
        [InlineData(true, typeof(ItemSurrogate7))]
        [InlineData(false, typeof(ItemSurrogate7))]
        [InlineData(true, typeof(ItemSurrogate8))]
        [InlineData(false, typeof(ItemSurrogate8))]
        [InlineData(true, typeof(ItemSurrogate9))]
        [InlineData(false, typeof(ItemSurrogate9))]
        public void TestImmutablePartialSerialization(bool compile,Type surrogate)
        {
            IKindRepository rep = new KindRepository();

            var item = new Item("Item1,", rep.Get(1));
            var serializer = new ItemSerializer(rep,compile,surrogate);

            MemoryStream ms = new MemoryStream();
            serializer.Serialize(ms, item);
            ms.Seek(0, SeekOrigin.Begin);
            var item2 = serializer.Deserialize(ms);
            Assert.Equal(rep.Get(1).Name, item2.Kind.Name);
        }


        //With 3 ways to write the 2 conversion functions there 9 ways to write a surrogate. The other 8 are below for completeness of the unit test

        [ProtoContract]
        public class ItemSurrogate2
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static Item Convert(ItemSurrogate2 s, SerializationContext context)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? ((IKindRepository)context.Context).Get(s.KindId.Value) : null);
            }
            public static ItemSurrogate2 Convert(Item o, SerializationContext context)
            {
                return o == null ? null : new ItemSurrogate2 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }

        [ProtoContract]
        public class ItemSurrogate3
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static Item Convert(ItemSurrogate3 s, SerializationContext context)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? ((IKindRepository)context.Context).Get(s.KindId.Value) : null);
            }
            public static implicit operator ItemSurrogate3(Item o)
            {
                return o == null ? null : new ItemSurrogate3 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }


        [ProtoContract]
        public class ItemSurrogate4
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static Item Convert(ItemSurrogate4 s)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? new KindRepository().Get(s.KindId.Value) : null);
            }

            public static ItemSurrogate4 Convert(Item o)
            {
                return o == null ? null : new ItemSurrogate4 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }

        [ProtoContract]
        public class ItemSurrogate5
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static Item Convert(ItemSurrogate5 s)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? new KindRepository().Get(s.KindId.Value) : null);
            }

            public static ItemSurrogate5 Convert(Item o, SerializationContext context)
            {
                return o == null ? null : new ItemSurrogate5 { Name = o.Name, KindId = o.Kind?.Id };
            }            
        }

        [ProtoContract]
        public class ItemSurrogate6
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static Item Convert(ItemSurrogate6 s)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? new KindRepository().Get(s.KindId.Value) : null);
            }

            public static implicit operator ItemSurrogate6(Item o)
            {
                return o == null ? null : new ItemSurrogate6 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }

        [ProtoContract]
        public class ItemSurrogate7
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static Item Convert(ItemSurrogate7 s)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? new KindRepository().Get(s.KindId.Value) : null);
            }

            public static ItemSurrogate7 Convert(Item o)
            {
                return o == null ? null : new ItemSurrogate7 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }

        [ProtoContract]
        public class ItemSurrogate8
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static implicit operator Item(ItemSurrogate8 s)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? new KindRepository().Get(s.KindId.Value) : null);
            }
            public static ItemSurrogate8 Convert(Item o, SerializationContext context)
            {
                return o == null ? null : new ItemSurrogate8 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }

        [ProtoContract]
        public class ItemSurrogate9
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public int? KindId { get; set; }

            public static implicit operator Item(ItemSurrogate9 s)
            {
                return s == null ? null : new Item(s.Name, s.KindId.HasValue ? new KindRepository().Get(s.KindId.Value) : null);
            }

            public static implicit operator ItemSurrogate9(Item o)
            {
                return o == null ? null : new ItemSurrogate9 { Name = o.Name, KindId = o.Kind?.Id };
            }
        }

    }
}
