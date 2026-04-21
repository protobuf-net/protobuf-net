// Regression tests for PR #1213 TODO:
// "regression inheritance + surrogate + interface; decide how to respond".
//
// Marc flagged this without leaving a failing test. Investigation confirms
// that the three features combine correctly across the plausible setups.
// The only hard constraint is a C# language rule (CS0552: user-defined
// conversion operators cannot involve interfaces) — which is orthogonal to
// protobuf-net. When an interface is on the conversion boundary, the
// surrogate must expose static converter methods marked with
// [ProtoConverter] (or use the internal SetSurrogate overload that takes
// explicit MethodInfo arguments); operators cannot be declared.
//
// Setups covered:
//   A. Interface is the registered root; surrogate configured on the interface
//      MetaType via [ProtoConverter] methods; concrete class is a sub-type
//      on the surrogate side.
//   B. Root is a class that happens to implement an interface; surrogate
//      configured on the root class via ordinary implicit operators.
//   C. Surrogate replaces a type whose members reference an interface; the
//      surrogate itself only exposes the concrete shape.
using ProtoBuf;
using ProtoBuf.Meta;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class InheritanceSurrogateInterfaceTests
    {
        // -------- Setup A: interface root, class impl, surrogate on interface root --------
        // C# forbids user-defined conversion ops involving interfaces (CS0552),
        // so the surrogate exposes [ProtoConverter] static methods instead.
        public interface IShape
        {
            int Id { get; set; }
        }

        public class Circle : IShape
        {
            public int Id { get; set; }
            public int Radius { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(1001, typeof(CircleSurrogate))]
        public class ShapeSurrogate
        {
            [ProtoMember(1)] public int Id { get; set; }

            [ProtoConverter]
            public static ShapeSurrogate FromShape(IShape value) => value switch
            {
                null => null,
                Circle c => new CircleSurrogate { Id = c.Id, Radius = c.Radius },
                _ => new ShapeSurrogate { Id = value.Id },
            };

            [ProtoConverter]
            public static IShape ToShape(ShapeSurrogate value) => value switch
            {
                null => null,
                CircleSurrogate c => new Circle { Id = c.Id, Radius = c.Radius },
                _ => null,
            };
        }

        [ProtoContract]
        public class CircleSurrogate : ShapeSurrogate
        {
            [ProtoMember(1)] public int Radius { get; set; }
        }

        [Fact]
        public void A_InterfaceRoot_SurrogateOnInterface_RoundTrips()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(IShape), false);
            model.Add(typeof(Circle), false).AddField(1, nameof(Circle.Radius));
            model[typeof(IShape)].AddSubType(1001, typeof(Circle));
            model[typeof(IShape)].SetSurrogate(typeof(ShapeSurrogate));

            var ms = new MemoryStream();
            model.Serialize<IShape>(ms, new Circle { Id = 1, Radius = 5 });
            ms.Position = 0;
            var result = model.Deserialize<IShape>(ms);

            var circle = Assert.IsType<Circle>(result);
            Assert.Equal(1, circle.Id);
            Assert.Equal(5, circle.Radius);
        }

        // -------- Setup B: class hierarchy where root class implements an interface, surrogate on root --------
        public interface ITag { int Tag { get; } }

        public class Animal : ITag
        {
            public int Tag { get; set; }
            public string Name { get; set; }
        }

        public class Dog : Animal
        {
            public string Breed { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(1001, typeof(DogSurrogate))]
        public class AnimalSurrogate
        {
            [ProtoMember(1)] public int Tag { get; set; }
            [ProtoMember(2)] public string Name { get; set; }

            public static implicit operator AnimalSurrogate(Animal v) => v switch
            {
                null => null,
                Dog d => new DogSurrogate { Tag = d.Tag, Name = d.Name, Breed = d.Breed },
                _ => new AnimalSurrogate { Tag = v.Tag, Name = v.Name },
            };

            public static implicit operator Animal(AnimalSurrogate v) => v switch
            {
                null => null,
                DogSurrogate d => new Dog { Tag = d.Tag, Name = d.Name, Breed = d.Breed },
                _ => new Animal { Tag = v.Tag, Name = v.Name },
            };
        }

        [ProtoContract]
        public class DogSurrogate : AnimalSurrogate
        {
            [ProtoMember(1)] public string Breed { get; set; }
        }

        [Fact]
        public void B_ClassRootImplementsInterface_SurrogateOnRoot_RoundTrips()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var a = model.Add(typeof(Animal), false);
            model.Add(typeof(Dog), false);
            a.AddSubType(1001, typeof(Dog));
            a.SetSurrogate(typeof(AnimalSurrogate));

            var ms = new MemoryStream();
            model.Serialize<Animal>(ms, new Dog { Tag = 7, Name = "Rex", Breed = "Lab" });
            ms.Position = 0;
            var result = (Animal)model.Deserialize(ms, null, typeof(Animal));

            var dog = Assert.IsType<Dog>(result);
            Assert.Equal(7, dog.Tag);
            Assert.Equal("Rex", dog.Name);
            Assert.Equal("Lab", dog.Breed);
        }

        // -------- Setup C: surrogate replaces a type whose members reference an interface --------
        public interface IPayload { }
        public class PayloadA : IPayload
        {
            public int X { get; set; }
        }

        public class Envelope
        {
            public IPayload Payload { get; set; }
        }

        [ProtoContract]
        public class EnvelopeSurrogate
        {
            [ProtoMember(1)] public int X { get; set; }

            public static implicit operator EnvelopeSurrogate(Envelope e) =>
                e is null ? null : new EnvelopeSurrogate { X = (e.Payload as PayloadA)?.X ?? 0 };

            public static implicit operator Envelope(EnvelopeSurrogate s) =>
                s is null ? null : new Envelope { Payload = new PayloadA { X = s.X } };
        }

        [Fact]
        public void C_SurrogateFlattensInterfaceMember_RoundTrips()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Envelope), false).SetSurrogate(typeof(EnvelopeSurrogate));

            var ms = new MemoryStream();
            model.Serialize(ms, new Envelope { Payload = new PayloadA { X = 42 } });
            ms.Position = 0;
            var result = (Envelope)model.Deserialize(ms, null, typeof(Envelope));

            var payload = Assert.IsType<PayloadA>(result.Payload);
            Assert.Equal(42, payload.X);
        }
    }
}
