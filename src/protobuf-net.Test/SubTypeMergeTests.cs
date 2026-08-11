using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// Merging behaviour when one field carries a sub-type marker more than once.
    /// </summary>
    /// <remarks>
    /// A payload may legitimately declare the same field twice — protobuf says a repeated singular
    /// message field merges — and with an inheritance hierarchy the two occurrences may name
    /// different layers. Specialising works; two *unrelated* siblings cannot, and used to take the
    /// process with them rather than failing.
    /// </remarks>
    public class SubTypeMergeTests
    {
        [ProtoContract]
        [ProtoInclude(10, typeof(Dog))]
        [ProtoInclude(11, typeof(Cat))]
        public class Animal
        {
            [ProtoMember(1)] public int Age { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(Puppy))]
        public class Dog : Animal
        {
            [ProtoMember(1)] public string Bark { get; set; }
        }

        [ProtoContract]
        public class Puppy : Dog
        {
            [ProtoMember(1)] public int Weeks { get; set; }
        }

        [ProtoContract]
        public class Cat : Animal
        {
            [ProtoMember(1)] public string Meow { get; set; }
        }

        [ProtoContract]
        public class Holder
        {
            [ProtoMember(1)] public Animal Pet { get; set; }
        }

        private static byte[] Encode(Animal pet)
        {
            using var ms = new MemoryStream();
            RuntimeTypeModel.Default.Serialize(ms, new Holder { Pet = pet });
            return ms.ToArray();
        }

        /// <summary>Concatenating two payloads is how a duplicated field is manufactured.</summary>
        private static Holder Decode(params Animal[] occurrences)
        {
            using var ms = new MemoryStream();
            foreach (var pet in occurrences)
            {
                var bytes = Encode(pet);
                ms.Write(bytes, 0, bytes.Length);
            }
            ms.Position = 0;
            return RuntimeTypeModel.Default.Deserialize<Holder>(ms);
        }

        /// <summary>
        /// The case that used to be an unrecoverable process kill: two different sub-types of one
        /// base. `StackOverflowException` cannot be caught, and this is reachable from untrusted
        /// input, so the only acceptable outcome is a catchable exception.
        /// </summary>
        [Fact]
        public void MergingUnrelatedSiblingsThrowsRatherThanRecursing()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Decode(new Dog { Bark = "woof" }, new Cat { Meow = "miaow" }));

            Assert.Contains("Dog", ex.Message);
            Assert.Contains("Cat", ex.Message);
        }

        /// <summary>Specialising is the case the merge exists for, and must keep working.</summary>
        [Fact]
        public void MergingABaseIntoADerivedStillWorks()
        {
            var holder = Decode(new Dog { Age = 3, Bark = "woof" }, new Puppy { Weeks = 6 });

            var puppy = Assert.IsType<Puppy>(holder.Pet);
            Assert.Equal(6, puppy.Weeks);
            Assert.Equal("woof", puppy.Bark);
            Assert.Equal(3, puppy.Age);
        }

        /// <summary>...and so must the other order, which never reaches the merge at all.</summary>
        [Fact]
        public void MergingADerivedIntoItsBaseStillWorks()
        {
            var holder = Decode(new Puppy { Age = 3, Weeks = 6 }, new Dog { Bark = "woof" });

            var puppy = Assert.IsType<Puppy>(holder.Pet);
            Assert.Equal(6, puppy.Weeks);
            Assert.Equal("woof", puppy.Bark);
        }

        /// <summary>The same type twice is an ordinary merge.</summary>
        [Fact]
        public void MergingTheSameSubTypeStillWorks()
        {
            var holder = Decode(new Dog { Age = 3 }, new Dog { Bark = "woof" });

            var dog = Assert.IsType<Dog>(holder.Pet);
            Assert.Equal(3, dog.Age);
            Assert.Equal("woof", dog.Bark);
        }
    }
}
