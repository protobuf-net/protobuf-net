using Examples.Ppt;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Examples
{
    [ProtoContract]
    class Entity
    {
        [ProtoMember(1)]
        public string Foo { get; set; }
    }

    class CustomEnumerable : IEnumerable<int>, ICollection<int>
    {
        private readonly List<int> items = new List<int>();
        IEnumerator<int> IEnumerable<int>.GetEnumerator() { return items.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return items.GetEnumerator(); }
        public void Add(int value) { items.Add(value); }

        // need ICollection<int> for Add to work
        bool ICollection<int>.IsReadOnly => false;
        void ICollection<int>.Clear() => items.Clear();
        int ICollection<int>.Count => items.Count;
        bool ICollection<int>.Contains(int item) => items.Contains(item);
        bool ICollection<int>.Remove(int item) => items.Remove(item);
        void ICollection<int>.CopyTo(int[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
    }
    [ProtoContract]
    class EntityWithPackedInts
    {
        public void ClearList()
        {
            List = null;
        }
        public EntityWithPackedInts() { List = new List<int>(); }
        [ProtoMember(1, Options = MemberSerializationOptions.Packed)]
        public List<int> List {get;private set;}

        [ProtoMember(2, Options = MemberSerializationOptions.Packed)]
        public List<int> ListNoDefault { get; set; }
        
        [ProtoMember(3, Options = MemberSerializationOptions.Packed)]
        public int[] ItemArray { get; set; }

        [ProtoMember(4, Options = MemberSerializationOptions.Packed)]
        public CustomEnumerable Custom { get; set; }
    }
    [ProtoContract]
    class EntityWithUnpackedInts
    {
        public EntityWithUnpackedInts() { Items = new List<int>(); }
        [ProtoMember(1)]
        public List<int> Items { get; private set; }

        [ProtoMember(2)]
        public List<int> ItemsNoDefault { get; set; }

        [ProtoMember(3)]
        public int[] ItemArray { get; set; }

        [ProtoMember(4)]
        public CustomEnumerable Custom { get; set; }
    }

    class MyList : List<Entity> { }

    
    public class ListTests
    {
        [Fact]
        public void ListOfByteArray()
        {
            var data = new List<byte[]> {
                new byte[] {0,1,2,3,4},
                new byte[] {5,6,7},
                new byte[] {8,9,10},
                new byte[] {}
            };
            var clone = Serializer.DeepClone(data);

            Assert.NotSame(data, clone);
            Assert.Equal(4, clone.Count);
            Assert.True(data[0].SequenceEqual(clone[0]));
            Assert.True(data[1].SequenceEqual(clone[1]));
            Assert.True(data[2].SequenceEqual(clone[2]));
            Assert.True(data[3].SequenceEqual(clone[3]));
        }

        [Fact]
        public void JaggedByteArray()
        {
            var data = new[] {
                new byte[] {0,1,2,3,4},
                new byte[] {5,6,7},
                new byte[] {8,9,10}
            };
            var clone = Serializer.DeepClone(data);

            Assert.NotSame(data, clone);
            Assert.Equal(3, clone.Length);
            Assert.True(data[0].SequenceEqual(clone[0]));
            Assert.True(data[1].SequenceEqual(clone[1]));
            Assert.True(data[2].SequenceEqual(clone[2]));
        }


        [Fact]
        public void TestUnpackedIntListLayout()
        {
            EntityWithUnpackedInts item = new EntityWithUnpackedInts {
                Items = {1,2,3,4,5,1000}
            };
            Assert.True(Program.CheckBytes(item, 08, 01, 08, 02, 08, 03, 08, 04, 08, 05, 08, 0xE8, 07));

            var clone = Serializer.DeepClone(item);
            Assert.NotSame(item.Items, clone.Items);
            Assert.True(item.Items.SequenceEqual(clone.Items));
        }

        [Fact]
        public void TestUnpackedIntArrayLayout()
        {
            EntityWithUnpackedInts item = new EntityWithUnpackedInts
            {
                ItemArray = new int[] { 1, 2, 3, 4, 5, 1000 }
            };
            Assert.True(Program.CheckBytes(item, 0x18, 01, 0x18, 02, 0x18, 03, 0x18, 04, 0x18, 05, 0x18, 0xE8, 07));

            var clone = Serializer.DeepClone(item);
            Assert.NotSame(item.ItemArray, clone.ItemArray);
            Assert.True(item.ItemArray.SequenceEqual(clone.ItemArray));
        }

        [Fact]
        public void TestUnpackedIntCustomLayout()
        {
            EntityWithUnpackedInts item = new EntityWithUnpackedInts
            {
                Custom = new CustomEnumerable { 1, 2, 3, 4, 5, 1000 }
            };
            Assert.True(Program.CheckBytes(item, 0x20, 01, 0x20, 02, 0x20, 03, 0x20, 04, 0x20, 05, 0x20, 0xE8, 07));

            var clone = Serializer.DeepClone(item);
            Assert.NotSame(item.Custom, clone.Custom);
            Assert.True(item.Custom.SequenceEqual(clone.Custom));
        }

        [Fact]
        public void TestPackedIntListLayout()
        {
            EntityWithPackedInts item = new EntityWithPackedInts
            {
                List = { 1, 2, 3, 4, 5, 1000}
            };
            Assert.True(Program.CheckBytes(item, 0x0A, 07, 01, 02, 03, 04, 05, 0xE8, 07));

            var clone = Serializer.DeepClone(item);
            Assert.NotSame(item.List, clone.List);
            Assert.True(item.List.SequenceEqual(clone.List));
        }

        [Fact]
        public void TestPackedIntArrayLayout()
        {
            EntityWithPackedInts item = new EntityWithPackedInts
            {
                ItemArray = new int[] { 1, 2, 3, 4, 5, 1000 }
            };
            item.ClearList();
            Assert.True(Program.CheckBytes(item, 0x1A, 07, 01, 02, 03, 04, 05, 0xE8, 07));

            var clone = Serializer.DeepClone(item);
            Assert.NotSame(item.ItemArray, clone.ItemArray);
            Assert.True(item.ItemArray.SequenceEqual(clone.ItemArray));
        }

        [Fact]
        public void TestPackedIntCustomLayout()
        {
            EntityWithPackedInts item = new EntityWithPackedInts
            {
                Custom = new CustomEnumerable { 1, 2, 3, 4, 5, 1000 }
            };
            item.ClearList();
            RuntimeTypeModel.Default.Add<CustomEnumerable>();
            Program.CheckBytes(item, "22-07-01-02-03-04-05-E8-07");

            var clone = Serializer.DeepClone(item);
            Assert.NotSame(item.Custom, clone.Custom);
            Assert.True(item.Custom.SequenceEqual(clone.Custom));
        }


        [Fact]
        public void SerializePackedDeserializeUnpacked()
        {
            EntityWithPackedInts item = new EntityWithPackedInts
            {
                List = { 1, 2, 3, 4, 5, 1000 }
            };
            EntityWithUnpackedInts clone = Serializer.ChangeType<EntityWithPackedInts, EntityWithUnpackedInts>(item);
            Assert.NotSame(item.List, clone.Items);
            Assert.True(item.List.SequenceEqual(clone.Items));
        }

        [Fact]
        public void SerializeUnpackedSerializePacked()
        {
            EntityWithUnpackedInts item = new EntityWithUnpackedInts
            {
                Items = { 1, 2, 3, 4, 5, 1000 }
            };
            EntityWithPackedInts clone = Serializer.ChangeType<EntityWithUnpackedInts, EntityWithPackedInts>(item);
            Assert.NotSame(item.Items, clone.List);
            Assert.True(item.Items.SequenceEqual(clone.List));
        }

        [Fact]
        public void UnpackedNullOrEmptyListDeserializesAsNull()
        {
            var item = new EntityWithUnpackedInts();
            Assert.Null(item.ItemsNoDefault);
            var clone = Serializer.DeepClone(item);
            Assert.Null(clone.ItemsNoDefault);

            item.ItemsNoDefault = new List<int>();
            clone = Serializer.DeepClone(item);
            Assert.Null(clone.ItemsNoDefault);

            item.ItemsNoDefault.Add(123);
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.ItemsNoDefault);
            Assert.Single(clone.ItemsNoDefault);
            Assert.Equal(123, clone.ItemsNoDefault[0]);
        }

        [Fact]
        public void PackedEmptyListDeserializesAsEmpty()
        {
            var item = new EntityWithPackedInts();
            Assert.Null(item.ListNoDefault);
            var clone = Serializer.DeepClone(item);
            Assert.Null(clone.ListNoDefault);
           
            item.ListNoDefault = new List<int>();
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.ListNoDefault);
            Assert.Empty(clone.ListNoDefault);

            item.ListNoDefault.Add(123);
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.ListNoDefault);
            Assert.Single(clone.ListNoDefault);
            Assert.Equal(123, clone.ListNoDefault[0]);
        }

        [Fact]
        public void UnpackedNullOrEmptyArrayDeserializesAsNull()
        {
            var item = new EntityWithUnpackedInts();
            Assert.Null(item.ItemArray);
            var clone = Serializer.DeepClone(item);
            Assert.Null(clone.ItemArray);

            item.ItemArray = new int[0];
            clone = Serializer.DeepClone(item);
            Assert.Null(clone.ItemArray);

            item.ItemArray = new int[1] { 123 };
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.ItemArray);
            Assert.Single(clone.ItemArray);
            Assert.Equal(123, clone.ItemArray[0]);
        }


        [Fact]
        public void PackedEmptyArrayDeserializesAsEmpty()
        {
            var item = new EntityWithPackedInts();
            Assert.Null(item.ItemArray);
            var clone = Serializer.DeepClone(item);
            Assert.Null(clone.ItemArray);

            item.ItemArray = new int[0];
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.ItemArray);
            Assert.Empty(clone.ItemArray);

            item.ItemArray = new int[1] { 123 };
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.ItemArray);
            Assert.Single(clone.ItemArray);
            Assert.Equal(123, clone.ItemArray[0]);
        }

        [Fact]
        public void UnpackedNullOrEmptyCustomDeserializesAsNull()
        {
            var item = new EntityWithUnpackedInts();
            Assert.Null(item.Custom);
            var clone = Serializer.DeepClone(item);
            Assert.Null(clone.Custom);

            item.Custom = new CustomEnumerable();
            clone = Serializer.DeepClone(item);
            Assert.Null(clone.Custom);

            item.Custom.Add(123);
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.Custom);
            Assert.Equal(123, item.Custom.Single());
        }

        [Fact]
        public void PackedEmptyCustomDeserializesAsEmpty()
        {
            var item = new EntityWithPackedInts();
            Assert.Null(item.Custom);
            var clone = Serializer.DeepClone(item);
            Assert.Null(clone.Custom);

            item.Custom = new CustomEnumerable();
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.Custom);
            Assert.Empty(clone.Custom);

            item.Custom.Add(123);
            clone = Serializer.DeepClone(item);
            Assert.NotNull(clone.Custom);
            Assert.Equal(123, item.Custom.Single());
        }

        [Fact]
        public void TestEmtpyBasicListOfEntity()
        {
            var foos = new List<Entity>();
            var clone = Serializer.DeepClone(foos);
            Assert.NotNull(clone);
        }

        [Fact]
        public void TestEmptyMyListOfEntity()
        {
            var foos = new MyList();
            var clone = Serializer.DeepClone(foos);
            Assert.NotNull(clone);
        }

        [Fact]
        public void TestNonEmtpyBasicListOfEntity()
        {
            var foos = new List<Entity>
            {
                new Entity { Foo = "abc"},
                new Entity { Foo = "def"},
            };
            var clone = Serializer.DeepClone(foos);
            Assert.NotNull(clone);
            Assert.NotSame(foos, clone);
            Assert.Equal(foos.GetType(), clone.GetType());
            Assert.Equal(2, clone.Count);
            Assert.Equal(foos[0].Foo, clone[0].Foo);
            Assert.Equal(foos[1].Foo, clone[1].Foo);
        }

        [Fact]
        public void TestNonEmptyMyListOfEntity()
        {
            var foos = new MyList() 
            {
                new Entity { Foo = "abc"},
                new Entity { Foo = "def"},
            };
            var clone = Serializer.DeepClone(foos);
            Assert.NotNull(clone);
            Assert.NotSame(foos, clone);
            Assert.Equal(foos.GetType(), clone.GetType());
            Assert.Equal(2, clone.Count);
            Assert.Equal(foos[0].Foo, clone[0].Foo);
            Assert.Equal(foos[1].Foo, clone[1].Foo);
        }
        
        [Theory]
        [InlineData(SerializerFeatures.WireTypeVarint, "22-10-08-FF-FF-FF-FF-FF-FF-FF-FF-FF-01-12-03-61-62-63-22-05-12-03-64-65-66-22-07-08-01-12-03-67-68-69")]
        /*
        22 = field 4, type String
        10 = length 16
           08 = field 1, type Varint
           FF-FF-FF-FF-FF-FF-FF-FF-FF-01 = -1 (raw)
           12 = field 2, type String
           03 = length 3
           61-62-63 = abc
        22 = field 4, type String
        05 = length 5
           12 = field 2, type String
           03 = length 3
           64-65-66 = def
        22 = field 4, type String
        07 = length 7
           08 = field 1, type Varint
           01 = 1 (raw)
           12 = field 2, type String
           03 = length 3
           67-68-69 = ghi
        */
        [InlineData(SerializerFeatures.WireTypeSignedVarint, "22-07-08-01-12-03-61-62-63-22-05-12-03-64-65-66-22-07-08-02-12-03-67-68-69")]
        /*
        22 = field 4, type String
        07 = length 7
           08 = field 1, type Varint
           01 = -1 (zigzag)
           12 = field 2, type String
           03 = length 3
           61-62-63 = abc
        22 = field 4, type String
           05 = length 5
           12 = field 2, type String
           03 = length 3
           64-65-66 = def
        22 = field 4, type String
        07 = length 7
           08 = field 1, type Varint
           02 = 1 (zigzag)
           12 = field 2, type String
           03 = length 3
           67-68-69 = ghi
        */
        public void ReadWriteMapWorks(SerializerFeatures keyFeatures, string expected)
        {
            using var ms = new MemoryStream();

            var data = new Dictionary<int, string>
            {
                { -1, "abc" },
                { 0, "def" },
                { 1, "ghi" },
            };

            var writer = ProtoWriter.State.Create(ms, null);
            try
            {
                MapSerializer.CreateDictionary<int,string>().WriteMap(ref writer, 4, default, data, keyFeatures, default);
                writer.Close();
            }
            catch
            {
                writer.Abandon();
                throw;
            }
            finally
            {
                writer.Dispose();
            }

            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal(expected, hex);
            ms.Position = 0;

            data = null;
            var reader = ProtoReader.State.Create(ms, null);
            try
            {
                int field;
                while((field = reader.ReadFieldHeader()) > 0)
                {
                    switch(field)
                    {
                        case 4:
                            data = MapSerializer.CreateDictionary<int, string>().ReadMap(ref reader, default, data, keyFeatures, default);
                            break;
                        default:
                            reader.SkipField();
                            break;
                    }
                }
            }
            finally
            {
                reader.Dispose();
            }
            Assert.NotNull(data);
            Assert.Equal(3, data.Count);

            var s = string.Join(", ", data.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
            Assert.Equal("-1=abc, 0=def, 1=ghi", s);
        }

        [Fact]
        public void TestCompositeDictionary()
        {
            DictionaryTestEntity obj = new DictionaryTestEntity
            {
                Foo = "bar",
                Stuff =
                {
                    {"abc", CompositeType.Create(123)},
                    {"def", CompositeType.Create(DateTime.Today)},
                    {"ghi", CompositeType.Create("hello world")},
                }
            }, clone = Serializer.DeepClone(obj);

            Assert.NotNull(clone);
            Assert.NotSame(clone, obj);
            Assert.Equal("bar", clone.Foo);
            Assert.Equal(3, clone.Stuff.Count);
            Assert.Equal(123, clone.Stuff["abc"].Value);
            Assert.Equal(DateTime.Today, clone.Stuff["def"].Value);
            Assert.Equal("hello world", clone.Stuff["ghi"].Value);
        }

        [ProtoContract]
        class DictionaryTestEntity
        {
            public DictionaryTestEntity() {
                Stuff = new CustomBox();
            }
            [ProtoMember(1)]
            public string Foo { get; set; }

            [ProtoMember(2)]
            public CustomBox Stuff { get; private set; }
        }


        class CustomBox : Dictionary<string, CompositeType>
        {
            
        } 

        [ProtoContract]
        [ProtoInclude(1, typeof(CompositeType<int>))]
        [ProtoInclude(2, typeof(CompositeType<DateTime>))]
        [ProtoInclude(3, typeof(CompositeType<string>))]
        abstract class CompositeType
        {
            public static CompositeType<T> Create<T>(T value)
            {
                return new CompositeType<T> { Value = value };
            }

            protected abstract object ValueImpl {get;set;}
            public object Value
            {
                get { return ValueImpl; }
                set { ValueImpl = value; } 
            }
        }
        [ProtoContract]
        class CompositeType<T> : CompositeType
        {
            [ProtoMember(1)]
            public new T Value { get; set; }

            protected override object ValueImpl
            {
                get { return Value; }
                set { Value = (T)value; }
            }
        }

        [Fact]
        public void TestListBytes()
        {
            List<Test3> list = new List<Test3> { new Test3 { C = new Test1 { A= 150} } };
            Assert.True(Program.CheckBytes(list, 0x0A, 0x05, 0x1a, 0x03, 0x08, 0x96, 0x01));
        }
        [Fact]
        public void TestListContents()
        {
            List<Test3> list = new List<Test3>
            {
                new Test3 { C = new Test1 { A = 123}},
                new Test3 { C = new Test1 { A = 456}},
                new Test3 { C = new Test1 { A = 789}}
            };
            
            var clone = Serializer.DeepClone(list);
            CheckLists(list, clone);
        }

        class Test3Enumerable : IEnumerable<Test3>, IProducerConsumerCollection<Test3>
        {
            private readonly List<Test3> items = new List<Test3>();


            public IEnumerator<Test3> GetEnumerator()
            {
                foreach (var item in items) yield return item;
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(Test3 item) { items.Add(item); }

            // for the Add API
            int ICollection.Count => items.Count;

            bool IProducerConsumerCollection<Test3>.TryAdd(Test3 value)
            {
                items.Add(value);
                return true;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0056:Use index operator", Justification = "Not on all TFMs")]
            bool IProducerConsumerCollection<Test3>.TryTake(out Test3 item)
            {
                if (items.Count == 0)
                {
                    item = default;
                    return false;
                }
                item = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return true;
            }

            object ICollection.SyncRoot => ((ICollection)items).SyncRoot;
            bool ICollection.IsSynchronized => ((ICollection)items).IsSynchronized;

            void ICollection.CopyTo(Array array, int index) => ((ICollection)items).CopyTo(array, index);

            Test3[] IProducerConsumerCollection<Test3>.ToArray() => items.ToArray();
            void IProducerConsumerCollection<Test3>.CopyTo(Test3[] array, int index) => items.CopyTo(array, index);

        }

        [Fact]
        public void TestEnumerableBytes()
        {
            Test3Enumerable list = new Test3Enumerable { new Test3 { C = new Test1 { A = 150 } } };
            Assert.True(Program.CheckBytes(list, 0x0A, 0x05, 0x1a, 0x03, 0x08, 0x96, 0x01));
        }

        [Fact]
        public void TestEnumerableContents()
        {
            Test3Enumerable items = new Test3Enumerable
            {
                new Test3 { C = new Test1 { A = 123}},
                new Test3 { C = new Test1 { A = 456}},
                new Test3 { C = new Test1 { A = 789}}
            };

            var clone = Serializer.DeepClone(items);
            CheckLists(items, clone);
        }

        [Fact]
        public void TestArrayBytes()
        {
            Test3[] list = new Test3[] { new Test3 { C = new Test1 { A = 150 } } };
            Assert.True(Program.CheckBytes(list, 0x0A, 0x05, 0x1a, 0x03, 0x08, 0x96, 0x01));
        }

        [Fact]
        public void TestArrayContents()
        {
            Test3[] arr = new Test3[]
            {
                new Test3 { C = new Test1 { A = 123}},
                new Test3 { C = new Test1 { A = 456}},
                new Test3 { C = new Test1 { A = 789}}
            };

            var clone = Serializer.DeepClone(arr);
            CheckLists(arr, clone);
        }

        [Fact]
        public void TestPackedArrayString()
        {
            using (var ms = new MemoryStream())
            {
                var obj = new ArrayOfPackedString { Items = new[] { "abc", "def", "ghi" } };
                Serializer.Serialize(ms, obj);
                ms.Position = 0;

                // silently ignores us, and just writes them as non-packed
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("0A-03-61-62-63-0A-03-64-65-66-0A-03-67-68-69", hex);

                var clone = Serializer.Deserialize<ArrayOfPackedString>(ms);
                Assert.True(clone.Items.SequenceEqual(new[] { "abc", "def", "ghi" }));
            }
        }
        [ProtoContract]
        class ArrayOfPackedString
        {
            [ProtoMember(1, Options = MemberSerializationOptions.Packed)]
            public string[] Items { get; set; }
        }
        [Fact]
        public void TestPackedListDateTime()
        {
            // it will just quietly ignore us
            var obj = new ListOfDateTime { Items = { new DateTime(2000, 1, 1) } };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            ms.Position = 0;
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("0A-04-08-9A-AB-01", hex); // == 10957 days expressed as zig-zag
            var clone = Serializer.Deserialize<ListOfDateTime>(ms);
            Assert.NotSame(obj, clone);
            var single = Assert.Single(clone.Items);
            Assert.Equal(new DateTime(2000, 1, 1), single);

        }
        [ProtoContract]
        class ListOfDateTime
        {
            [ProtoMember(1, Options = MemberSerializationOptions.Packed)]
            public List<DateTime> Items { get; } = new List<DateTime>();
        }
        [Fact]
        public void TestPackedCustomOfSubMessage()
        {
            // it will just quietly ignore us
            var obj = new CustomOfSubMessage { Items = { new CustomItem { }, new CustomItem { }, new CustomItem { } } };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            ms.Position = 0;
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("0A-00-0A-00-0A-00", hex);
            var clone = Serializer.Deserialize<CustomOfSubMessage>(ms);
            Assert.NotSame(obj, clone);
            Assert.Equal(3, clone.Items.Count);
        }

        [ProtoContract]
        class CustomOfSubMessage
        {
            [ProtoMember(1, Options = MemberSerializationOptions.Packed)]
            public CustomCollection Items { get; } = new CustomCollection();
        }
        [ProtoContract]
        class CustomItem { }
        class CustomCollection : List<CustomItem>
        {
        }

        class Test3Comparer : IEqualityComparer<Test3>
        {

            public bool Equals(Test3 x, Test3 y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                if (ReferenceEquals(x.C, y.C)) return true;
                if (x.C == null || y.C == null) return false;
                return x.C.A == y.C.A;
            }
            public int GetHashCode(Test3 obj)
            {
                throw new System.NotImplementedException();
            }


        }
        static void CheckLists(IEnumerable<Test3> original, IEnumerable<Test3> clone)
        {
            Assert.True(original.SequenceEqual(clone,new Test3Comparer()));
        }

        [Fact]
        public void CheckNakedLinkedListCanRoundtrip()
        {
            var list = new LinkedList<BasicItem>();
            list.AddLast(new BasicItem{Value="abc"});
            list.AddLast(new BasicItem{Value="def"});
            var clone = Serializer.DeepClone(list);
            Assert.Equal(2, clone.Count);
            Assert.Equal("abc", clone.First.Value.Value);
            Assert.Equal("def", clone.Last.Value.Value);
        }
        [Fact]
        public void CheckWrappedLinkedListCanRoundtrip()
        {
            var wrapper = new WithLinkedList();
            wrapper.Items.AddLast(new BasicItem { Value = "abc" });
            wrapper.Items.AddLast(new BasicItem { Value = "def" });
            var clone = Serializer.DeepClone(wrapper);
            Assert.Equal(2, clone.Items.Count);
            Assert.Equal("abc", clone.Items.First.Value.Value);
            Assert.Equal("def", clone.Items.Last.Value.Value);
        }
        [ProtoContract]
        class BasicItem
        {
            [ProtoMember(1)]
            public string Value { get; set; }
        }
        [ProtoContract]
        class WithLinkedList
        {
            [ProtoMember(1)]
            public LinkedList<BasicItem> Items { get; private set; }

            public WithLinkedList()
            {
                Items = new LinkedList<BasicItem>();
            }
        }
    }
}
