using Examples;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class InheritanceExtensible
    {
        [Fact]
        public void CanRoundTripTrivialExtensibleModel_NonCompiled()
            => CanRoundTrip<SomeTrivialType>(model => model);

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_CompileInPlace()
            => CanRoundTrip<SomeTrivialType>(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_Compile()
            => CanRoundTrip<SomeTrivialType>(model => model.Compile());

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_CompileDll()
            => CanRoundTrip<SomeTrivialType>(model =>
            {
                var compiled = model.Compile(nameof(CanRoundTripTrivialExtensibleModel_CompileDll), nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                PEVerify.AssertValid(nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                return compiled;
            });

        [Fact]
        public void CanReliablyRoundTripRetainingData_NonCompiled()
            => CanReliablyRoundTripRetainingData(model => model);

        [Fact]
        public void CanReliablyRoundTripRetainingData_CompileInPlace()
            => CanReliablyRoundTripRetainingData(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanReliablyRoundTripRetainingData_Compile()
            => CanReliablyRoundTripRetainingData(model => model.Compile());

        [Fact]
        public void CanReliablyRoundTripRetainingData_CompileDll()
            => CanReliablyRoundTripRetainingData(model =>
            {
                var compiled = model.Compile(nameof(CanRoundTripTrivialExtensibleModel_CompileDll), nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                PEVerify.AssertValid(nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                return compiled;
            });

        [Fact]
        public void CanRoundTripBasicExtensibleModel_NonCompiled()
            => CanRoundTrip<SomeMiddleType>(model => model);

        [Fact]
        public void CanRoundTripBasicExtensibleModel_CompileInPlace()
            => CanRoundTrip<SomeMiddleType>(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanRoundTripBasicExtensibleModel_Compile()
            => CanRoundTrip<SomeMiddleType>(model => model.Compile());

        [Fact]
        public void CanRoundTripBasicExtensibleModel_CompileDll()
            => CanRoundTrip<SomeMiddleType>(model =>
            {
                var compiled = model.Compile(nameof(CanRoundTripBasicExtensibleModel_CompileDll), nameof(CanRoundTripBasicExtensibleModel_CompileDll) + ".dll");
                PEVerify.AssertValid(nameof(CanRoundTripBasicExtensibleModel_CompileDll) + ".dll");
                return compiled;
            });

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void SimpleExtensionDataUsage()
        {
            var obj = new SomeLeafType { When = new DateTime(2022, 01, 26, 10, 00, 00) };
            obj.AppendValue(40, "a");
            obj.AppendValue(41, "b", typeof(SomeLeafType));
            obj.AppendValue(42, "c", typeof(SomeMiddleType));
            obj.AppendValue(43, "d", typeof(SomeBaseType));

            Assert.Equal("a", obj.GetValue<string>(40));
            Assert.Equal("b", obj.GetValue<string>(41, typeof(SomeLeafType)));
            Assert.Equal("c", obj.GetValue<string>(42, typeof(SomeMiddleType)));
            Assert.Equal("d", obj.GetValue<string>(43, typeof(SomeBaseType)));

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("52-16-52-10-0A-06-08-A0-B7-C4-8F-06-C2-02-01-61-CA-02-01-62-D2-02-01-63-DA-02-01-64", hex);

            /*
52-16                           field 10 SomeBaseType -> SomeMiddleType (len 22)
    52-10                       field 10 SomeMiddleType -> SomeLeafType (len 16)
        0A-06                   field 1 When (len 6)
            08-A0-B7-C4-8F-06   field 1 seconds = 1643191200
        C2-02-01-61             field 40 = "a"
        CA-02-01-62             field 41 = "b"
    D2-02-01-63                 field 42 = "c"
DA-02-01-64                     field 43 = "d"
             */

            // just to prove that "seconds" value
            var seconds = (obj.When - Epoch).TotalSeconds;
            Assert.Equal(1643191200, seconds);

            ms.Position = 0;

            var clone = Serializer.Deserialize<SomeLeafType>(ms);
            Assert.NotSame(obj, clone);

            Assert.Equal("a", clone.GetValue<string>(40));
            Assert.Equal("b", clone.GetValue<string>(41, typeof(SomeLeafType)));
            Assert.Equal("c", clone.GetValue<string>(42, typeof(SomeMiddleType)));
            Assert.Equal("d", clone.GetValue<string>(43, typeof(SomeBaseType)));
        }

        [Fact]
        public void SimpleCustomExtensionDataUsage()
        {
            var obj = new CustomLeafType { When = new DateTime(2022, 01, 26, 10, 00, 00) };
            obj.AppendValue(40, "a");
            obj.AppendValue(41, "b", typeof(CustomLeafType));
            obj.AppendValue(42, "c", typeof(CustomMiddleType));
            obj.AppendValue(43, "d", typeof(CustomBaseType));

            Assert.Equal("a", obj.GetValue<string>(40));
            Assert.Equal("b", obj.GetValue<string>(41, typeof(CustomLeafType)));
            Assert.Equal("c", obj.GetValue<string>(42, typeof(CustomMiddleType)));
            Assert.Equal("d", obj.GetValue<string>(43, typeof(CustomBaseType)));

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("52-16-52-10-0A-06-08-A0-B7-C4-8F-06-C2-02-01-61-CA-02-01-62-D2-02-01-63-DA-02-01-64", hex);

            /*
52-16                           field 10 CustomBaseType -> CustomMiddleType (len 22)
    52-10                       field 10 CustomMiddleType -> CustomLeafType (len 16)
        0A-06                   field 1 When (len 6)
            08-A0-B7-C4-8F-06   field 1 seconds = 1643191200
        C2-02-01-61             field 40 = "a"
        CA-02-01-62             field 41 = "b"
    D2-02-01-63                 field 42 = "c"
DA-02-01-64                     field 43 = "d"
             */

            // just to prove that "seconds" value
            var seconds = (obj.When - Epoch).TotalSeconds;
            Assert.Equal(1643191200, seconds);

            ms.Position = 0;

            var clone = Serializer.Deserialize<CustomLeafType>(ms);
            Assert.NotSame(obj, clone);

            Assert.Equal("a", clone.GetValue<string>(40));
            Assert.Equal("b", clone.GetValue<string>(41, typeof(CustomLeafType)));
            Assert.Equal("c", clone.GetValue<string>(42, typeof(CustomMiddleType)));
            Assert.Equal("d", clone.GetValue<string>(43, typeof(CustomBaseType)));
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Extensible))]
        [InlineData(typeof(string))]
        [InlineData(typeof(SomeLeafType))]
        [InlineData(typeof(SomeRandomDerivedType))]
        public void DetectInvalidTargetInheritanceType(Type type)
        {
            var obj = new SomeMiddleType();
            var ex = Assert.Throws<InvalidOperationException>(() => obj.GetValue<string>(40, type));
            Assert.Equal($"The extension field target type '{type.FullName}' is not a valid base-type of 'ProtoBuf.InheritanceExtensible+SomeMiddleType'", ex.Message);
        }

        [Theory]
        [InlineData(typeof(ISomeInterface))]
        [InlineData(typeof(Foo))]
        public void DetectInvalidTargetObjectType(Type type)
        {
            var obj = new SomeMiddleType();
            var ex = Assert.Throws<NotSupportedException>(() => obj.GetValue<string>(40, type));
            Assert.Equal($"Extension fields can only be used with class target types ('{type.FullName}' is not valid)", ex.Message);
        }

        class SomeRandomDerivedType : SomeMiddleType { }
        interface ISomeInterface { }

        [Fact]
        public void DetectNullInstance()
        {
            SomeMiddleType obj = null;
            var ex = Assert.Throws<ArgumentNullException>(() => obj.GetValue<string>(40));
            Assert.Equal("instance", ex.ParamName);
        }

        [Fact]
        public void DetectValueTypeInstance()
        {
            var obj = new Foo();
            var ex = Assert.Throws<NotSupportedException>(() => obj.GetValue<string>(40));
            Assert.Equal($"Extension fields can only be used with class target types ('{typeof(Foo).FullName}' is not valid)", ex.Message);
        }

        [ProtoContract]
        struct Foo : ITypedExtensible
        {
            IExtension ITypedExtensible.GetExtensionObject(Type type, bool createIfMissing)
                => throw new NotImplementedException("shouldn't be called");
        }

        private void CanRoundTrip<T>(Func<RuntimeTypeModel, TypeModel> callback) where T : class, IExtensible, new()
        {
            var original = new T();
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<T>();
            var finalModel = callback(model);

            Extensible.AppendValue(finalModel, original, 99, "hello");
            var clone = finalModel.DeepClone(original);
            Assert.NotSame(original, clone);
            Assert.Equal("hello", Extensible.GetValue<string>(clone, 99));
        }

        private void CanReliablyRoundTripRetainingData(Func<RuntimeTypeModel, TypeModel> callback)
        {
            var original = new SomeRichLeafType
            {
                A = 12,
                B = 42,
                C = 92
            };
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<SomeRichBaseType>();
            model.Add<SomeBaseType>();
            var finalModel = callback(model);

            SomeRichLeafType clone;
            using (var ms = new MemoryStream())
            {
                finalModel.Serialize(ms, original);
                ms.Position = 0;
                var raw = finalModel.Deserialize<SomeBaseType>(ms);

                ms.Position = 0;
                ms.SetLength(0);

                finalModel.Serialize(ms, raw);
                ms.Position = 0;
                clone = (SomeRichLeafType)finalModel.Deserialize<SomeRichBaseType>(ms);
            }
            Assert.NotSame(original, clone);

            Assert.Equal(12, clone.A);
            Assert.Equal(42, clone.B);
            Assert.Equal(92, clone.C);
        }


        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class SomeTrivialType : Extensible
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeMiddleType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public abstract class SomeBaseType : Extensible
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeLeafType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class SomeMiddleType : SomeBaseType
        {
            [ProtoMember(1)]
            public string Message { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class SomeLeafType : SomeMiddleType
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(CustomMiddleType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public abstract class CustomBaseType : ITypedExtensible
        {
            IExtension extension;

            [ProtoMember(1)]
            public int Id { get; set; }

            IExtension ITypedExtensible.GetExtensionObject(Type type, bool createIfMissing)
                => Extensible.GetExtensionObject(ref extension, type, createIfMissing);
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(CustomLeafType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class CustomMiddleType : CustomBaseType
        {
            [ProtoMember(1)]
            public string Message { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class CustomLeafType : CustomMiddleType
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeRichMiddleType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public abstract class SomeRichBaseType : Extensible
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public int A { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeRichLeafType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class SomeRichMiddleType : SomeRichBaseType
        {
            [ProtoMember(1)]
            public string Message { get; set; }

            [ProtoMember(2)]
            public int B { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class SomeRichLeafType : SomeRichMiddleType
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }

            [ProtoMember(2)]
            public int C { get; set; }
        }
    }
}
