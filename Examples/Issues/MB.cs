﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Reflection.Emit;
using System.Reflection;
using ProtoBuf;

namespace TestMediaBrowser
{

    #region test data

    enum Fur { smooth, fluffy }

    [ProtoContract]
    [ProtoInclude(10, typeof(Animal))]
    class Thing
    {
        [ProtoMember(1)]
        public int Age;
    }
    [ProtoContract]
    [ProtoInclude(10, typeof(Dog))]
    class Animal : Thing
    {

        public Animal()
        {
            Random r = new Random();
            legs = r.Next();
            Weight = r.Next();
        }

        [ProtoMember(1)]
        private int legs;
        public int Legs { get { return legs; } }

        [ProtoMember(2)]
        public int Weight { get; private set; }
    }
    [ProtoContract]
    class Dog : Animal
    {

        // its used during reflection tests 
#pragma warning disable 169
        Fur i;
#pragma warning restore 169

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
        int? age;
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
        public List<MisterNullable> nullables { get; set; }
    }
    [ProtoContract]
    class DateTimeClass
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

    [TestFixture]
    public class TestSerialization
    {

        [Test]
        public void TestSerializerShouldSupportNulls()
        {
            var nestor = new Nestor();
            var clone = Serializer.DeepClone(nestor);
            Assert.IsNull(clone.nesty);
            Assert.IsNull(clone.Nesty2);
        }

        [Test]
        public void TestSerializerSupportForNestedObjects()
        {
            var nestor = new Nestor();
            nestor.nesty = new Nesty() { i = 99 };
            nestor.Nesty2 = new Nesty() { i = 100 };

            var clone = Serializer.DeepClone(nestor);

            Assert.AreEqual(99, clone.nesty.i);
            Assert.AreEqual(100, clone.Nesty2.i);

        }


        [Test]
        public void TestUninitializedDatetimePersistance()
        {
            var original = new DateTimeClass();
            var copy = Serializer.DeepClone(original);
            Assert.AreEqual(original.Date, copy.Date);
        }

        [Test]
        public void TestInheritedClone()
        {
            Thing original = new Animal();
            Assert.IsInstanceOfType(typeof(Animal), Serializer.DeepClone(original));
        }
        /*
        [Test]
        public void TestMergeDoesNotInventFields()
        {
            Series series = new Series();
            Series other = new Series();
            Serializer.Merge(series, other);

            Assert.IsNull(series.TVDBSeriesId);
            Assert.IsNull(other.TVDBSeriesId);
        }*/

        [Test, Ignore("This works differently by design; perhaps reverse order?")]
        public void TestMerging()
        {
            var source = new MisterNullable(11);
            source.Weight = 100;
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, source);
            ms.Position = 0;
            var target = new MisterNullable(22);

            target = Serializer.Merge(ms, target);
            Assert.AreEqual(100, target.Weight);
            Assert.AreEqual(22, target.Age);
        }

        [Test]
        public void TestListPersistance()
        {
            Listy l = new Listy();
            l.animals = new List<Animal>();
            l.animals.Add(new Dog());
            l.animals.Add(new Animal());

            var copy = Serializer.DeepClone(l);
            Assert.AreEqual(2, copy.animals.Count());
            Assert.AreEqual(copy.animals[0].GetType(), typeof(Dog));
        }

        [Test]
        public void TestNullableSerialization()
        {
            var nullable = new MisterNullable(99) { Weight = 2.2 };
            var copy = Serializer.DeepClone(nullable);

            Assert.AreEqual(nullable.Weight, copy.Weight);
            Assert.AreEqual(nullable.Age, copy.Age);

            nullable.Weight = null;

            copy = Serializer.DeepClone(nullable);
            Assert.IsNull(copy.Weight);
        }

        [Test]
        public void TestInheritedSerialization()
        {
            var dog = new Dog() { Fur = Fur.smooth, Age = 99, DontSaveMe = "bla" };

            var clone = Serializer.DeepClone(dog);

            Assert.AreEqual(dog.Fur, clone.Fur);
            Assert.AreEqual(dog.Age, clone.Age);
            Assert.AreEqual(dog.Legs, clone.Legs);
            Assert.AreEqual(dog.Weight, clone.Weight);
            Assert.IsNull(clone.DontSaveMe);
        }
        /*
        [Test]
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
            Assert.AreEqual(foo, foo2);
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

        [Test]
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
            Assert.AreEqual(foo, foo2);
        }

        [Test]
        public void BenchmarkPerformance()
        {
            List<DummyPersistanceObject> list = new List<DummyPersistanceObject>();
            for (int i = 0; i < 100000; i++)
            {
                list.Add(new DummyPersistanceObject() { Bar1 = i, Bar2 = "hello" });
            }

            TimeAction("Standard serialization", () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, list);
                    ms.Position = 0;
                    list = new List<DummyPersistanceObject>();
                    list = (List<DummyPersistanceObject>)bf.Deserialize(ms);
                }
            });

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
                using (MemoryStream ms = new MemoryStream())
                {
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

                }
            });


        }


        static void TimeAction(string description, Action func)
        {
            var watch = new Stopwatch();
            watch.Start();
            func();
            watch.Stop();
            Console.Write(description);
            Console.WriteLine(" Time Elapsed {0} ms", watch.ElapsedMilliseconds);
        }
    }
}