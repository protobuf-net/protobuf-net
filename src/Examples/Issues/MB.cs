using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.IO;
#if !COREFX
using System.Runtime.Serialization.Formatters.Binary;
#endif
using System.Diagnostics;
using ProtoBuf;
using ProtoBuf.Meta;
using Examples;
using Xunit.Abstractions;

namespace TestMediaBrowser
{

    #region test data

    public enum Fur { smooth, fluffy }

    [ProtoContract]
    [ProtoInclude(10, typeof(Animal))]
    public class Thing
    {
        [ProtoMember(1)]
        public int Age;
    }
    [ProtoContract]
    [ProtoInclude(10, typeof(Dog))]
    public class Animal : Thing
    {

        public Animal()
        {
            Random r = new Random();
            Legs = r.Next();
            Weight = r.Next();
        }


        //#pragma warning disable IDE0044 // Add readonly modifier
        //        [ProtoMember(1)]
        //        private int legs;
        //#pragma warning restore IDE0044 // Add readonly modifier

        [ProtoMember(1)]
        public int Legs { get; set; }

        [ProtoMember(2)]
        public int Weight { get; set; }
    }
    [ProtoContract]
    public class Dog : Animal
    {

        // its used during reflection tests 
#pragma warning disable 169, IDE0044, IDE0051
        Fur i;
#pragma warning restore 169, IDE0044, IDE0051

        [ProtoMember(1)]
        public Fur Fur { get; set; }
        public string DontSaveMe { get; set; }
    }
    [ProtoContract]
    class MisterNullable
    {

        public MisterNullable() { }

        public MisterNullable(int? age)
        {
            this.age = age;
        }

        [ProtoMember(1)]
#pragma warning disable IDE0044 // Add readonly modifier
        int? age;
#pragma warning restore IDE0044 // Add readonly modifier
        public int? Age { get { return age; } }

        [ProtoMember(2)]
        public double? Weight { get; set; }

        public static void WriteNullable(MisterNullable ms, BinaryWriter bw)
        {
            bool isNull = ms.age == null;
            bw.Write(isNull);
            if (!isNull)
            {
                bw.Write((int)ms.age);
            }
        }
    }
    [ProtoContract]
    class Listy
    {
        [ProtoMember(1)]
        public List<Animal> animals;
        [ProtoMember(2)]
        public List<MisterNullable> Nullables { get; set; }
    }
    [ProtoContract]
    public class DateTimeClass
    {
        [ProtoMember(1)]
        public DateTime Date { get; set; }
    }
    [ProtoContract]
    class Nesty
    {
        [ProtoMember(1)]
        public int i;
    }
    [ProtoContract]
    class Nestor
    {
        [ProtoMember(1)]
        public Nesty nesty;

        [ProtoMember(2)]
        public Nesty Nesty2 { get; set; }
    }

#endregion

    public class TestSerialization
    {
        private ITestOutputHelper Log { get; }
        public TestSerialization(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void TestSerializerShouldSupportNulls()
        {
            var nestor = new Nestor();
            var clone = Serializer.DeepClone(nestor);
            Assert.Null(clone.nesty);
            Assert.Null(clone.Nesty2);
        }

        [Fact]
        public void TestSerializerSupportForNestedObjects()
        {
            var nestor = new Nestor
            {
                nesty = new Nesty() { i = 99 },
                Nesty2 = new Nesty() { i = 100 }
            };

            var clone = Serializer.DeepClone(nestor);

            Assert.Equal(99, clone.nesty.i);
            Assert.Equal(100, clone.Nesty2.i);

        }

#if EMIT_DLL
        [Fact]
        public void CompileDateTimeClassModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(DateTimeClass));
            model.Compile("CompileDateTimeClassModel", "CompileDateTimeClassModel.dll");
            PEVerify.AssertValid("CompileDateTimeClassModel.dll");
        }
#endif

        [Fact]
        public void TestUninitializedDatetimePersistance()
        {
            var original = new DateTimeClass();
            Assert.NotNull(original);
            var copy = Serializer.DeepClone(original);
            Assert.NotNull(copy);
            Assert.Equal(original.Date, copy.Date);
        }

        [Fact]
        public void TestInheritedClone()
        {
            Thing original = new Animal();
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Thing), true);

            Assert.IsType<Animal>(model.DeepClone(original));
            model.CompileInPlace();
            Assert.IsType<Animal>(model.DeepClone(original));

            var compiled = model.Compile();
            Assert.IsType<Animal>(compiled.DeepClone(original));
        }

        [Fact]
        public void TestInheritedClone_PEVerify()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Thing), true);
            model.Compile("TestInheritedClone", "TestInheritedClone.dll");
            PEVerify.AssertValid("TestInheritedClone.dll");
        }

        /*
        [Fact]
        public void TestMergeDoesNotInventFields()
        {
            Series series = new Series();
            Series other = new Series();
            Serializer.Merge(series, other);

            Assert.Null(series.TVDBSeriesId);
            Assert.Null(other.TVDBSeriesId);
        }*/

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "This works differently by design; perhaps reverse order?")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void TestMerging()
        {
            var source = new MisterNullable(11)
            {
                Weight = 100
            };
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, source);
            ms.Position = 0;
            var target = new MisterNullable(22);

            target = Serializer.Merge(ms, target);
            Assert.Equal(100, target.Weight);
            Assert.Equal(22, target.Age);
        }

        [Fact]
        public void TestListPersistance()
        {
            Listy l = new Listy
            {
                animals = new List<Animal>
                {
                    new Dog(),
                    new Animal()
                }
            };

            var copy = Serializer.DeepClone(l);
            Assert.Equal(2, copy.animals.Count);
            Assert.Equal(typeof(Dog), copy.animals[0].GetType());
        }

        [Fact]
        public void TestNullableSerialization()
        {
            var nullable = new MisterNullable(99) { Weight = 2.2 };
            var copy = Serializer.DeepClone(nullable);

            Assert.Equal(nullable.Weight, copy.Weight);
            Assert.Equal(nullable.Age, copy.Age);

            nullable.Weight = null;

            copy = Serializer.DeepClone(nullable);
            Assert.Null(copy.Weight);
        }

        [Fact]
        public void TestInheritedSerialization()
        {
            var dog = new Dog() { Fur = Fur.smooth, Age = 99, DontSaveMe = "bla" };

            var clone = Serializer.DeepClone(dog);

            Assert.Equal(dog.Fur, clone.Fur);
            Assert.Equal(dog.Age, clone.Age);
            Assert.Equal(dog.Legs, clone.Legs);
            Assert.Equal(dog.Weight, clone.Weight);
            Assert.Null(clone.DontSaveMe);
        }
        /*
        [Fact]
        public void TestLateBoundSerialization()
        {
            DummyPersistanceObject foo = new DummyPersistanceObject() { Bar1 = 111, Bar2 = "hello" };
            DummyPersistanceObject foo2;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                ms.Position = 0;
                foo2 = Serializer.Deserialize<DummyPersistanceObject>(ms);
            }
            Assert.Equal(foo, foo2);
        }

         */
        [ProtoContract]
        [Serializable]
        public class DummyPersistanceObject : IEquatable<DummyPersistanceObject>
        {
            public override bool Equals(object obj)
            {
                return Equals(obj as DummyPersistanceObject);
            }
            public bool Equals(DummyPersistanceObject other)
            {
                return other != null && other.Bar1 == this.Bar1 && other.Bar2 == this.Bar2;
            }
            public override int GetHashCode()
            {
                return Bar1.GetHashCode() + 17 * (Bar2 ?? "").GetHashCode();
            }
            [ProtoMember(1)]
            public int Bar1 { get; set; }
            [ProtoMember(2)]
            public string Bar2 { get; set; }
        }

        static class GenericSerializer<T>
        {
            public static void Serialize(T instance, Stream destination)
            {
                Serializer.Serialize<T>(destination, instance);
            }
            public static T Deserialize(Stream source)
            {
                return Serializer.Deserialize<T>(source);
            }
        }

        [Fact]
        public void TestSerializer()
        {
            DummyPersistanceObject foo = new DummyPersistanceObject() { Bar1 = 111, Bar2 = "hello" };
            DummyPersistanceObject foo2;
            using (MemoryStream ms = new MemoryStream())
            {
                GenericSerializer<DummyPersistanceObject>.Serialize(foo, ms);
                ms.Position = 0;
                foo2 = GenericSerializer<DummyPersistanceObject>.Deserialize(ms);
            }
            Assert.Equal(foo, foo2);
        }

        [Fact]
        public void BenchmarkPerformance()
        {
            List<DummyPersistanceObject> list = new List<DummyPersistanceObject>();
            for (int i = 0; i < 100000; i++)
            {
                list.Add(new DummyPersistanceObject() { Bar1 = i, Bar2 = "hello" });
            }
#if !COREFX
            TimeAction("Standard serialization", () =>
            {
                using MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, list);
                ms.Position = 0;
                list = new List<DummyPersistanceObject>();
                list = (List<DummyPersistanceObject>)bf.Deserialize(ms);
            });
#endif
            GC.Collect();
            /*
            TimeAction("Manual Serialization", () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter bw = new BinaryWriter(ms);
                    foreach (var foo in list)
                    {
                        foo.Write(bw);
                    }
                    ms.Position = 0;
                    BinaryReader reader = new BinaryReader(ms);
                    list = new List<DummyPersistanceObject>();
                    for (int i = 0; i < 100000; i++)
                    {
                        list.Add(DummyPersistanceObject.Read(reader));
                    }
                }
            });
            */
            GC.Collect();

            TimeAction("Custom Serializer", () =>
            {
                using MemoryStream ms = new MemoryStream();
                foreach (var foo in list)
                {
                    Serializer.Serialize(ms, foo);
                }

                ms.Position = 0;

                list = new List<DummyPersistanceObject>();
                for (int i = 0; i < 100000; i++)
                {
                    list.Add(Serializer.Deserialize<DummyPersistanceObject>(ms));
                }
            });


        }

        private void TimeAction(string description, Action func)
        {
            var watch = new Stopwatch();
            watch.Start();
            func();
            watch.Stop();
            Log.WriteLine("{0} Time Elapsed {1} ms", description, watch.ElapsedMilliseconds);
        }
    }
}