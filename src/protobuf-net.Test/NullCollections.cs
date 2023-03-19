﻿using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;
using static ProtoBuf.Test.BufferWriteCountTests;

#nullable enable

namespace ProtoBuf.Test
{
    public class NullCollections
    {
        public ITestOutputHelper Log { get; }

        public NullCollections(ITestOutputHelper log)
            => Log = log;

        [Fact]
        public void MarkedNullWrappedOnNonCollectionShouldFail()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<InvalidMarkedNullWrappedCollection>();
            using var ms = new MemoryStream();
            var ex = Assert.Throws<NotSupportedException>(() => model.Serialize(ms, new InvalidMarkedNullWrappedCollection()));
            Assert.Equal("NullWrappedCollection can only be used with collection types", ex.Message);
        }

        [Theory]
        [InlineData(typeof(WithNullWrappedCollection))]
        [InlineData(typeof(WithNullWrappedGroupCollection))]
        public void SchemaGenerationFails_NotCurrentlySupported(Type type)
        {
            // this should look comparable to the ones below in SchemaGenerationSucceeds
            var model = RuntimeTypeModel.Create();
            model.Add(type);
            var ex = Assert.Throws<NotSupportedException>(() => model.GetSchema(type, ProtoSyntax.Proto3));
            Assert.Equal("Schema generation for null-wrapped collections is not currently implemented; poke @mgravell with a big stick if you need this!", ex.Message);
        }

        [Theory]
        [InlineData(typeof(Vanilla), @"syntax = ""proto3"";
package ProtoBuf.Test;

message Foo {
   int32 Id = 1;
}
message Vanilla {
   repeated Foo Foos = 4;
}
")]
        [InlineData(typeof(VanillaWrappedValue), @"syntax = ""proto3"";
package ProtoBuf.Test;

message Foo {
   int32 Id = 1;
}
message WrappedFoo {
   optional Foo value = 1;
}
message VanillaWrappedValue {
   repeated WrappedFoo Foos = 4;
}
")]
        [InlineData(typeof(VanillaWrappedGroupValue), @"syntax = ""proto3"";
package ProtoBuf.Test;

message Foo {
   int32 Id = 1;
}
message WrappedAsGroupFoo {
   optional Foo value = 1;
}
message VanillaWrappedGroupValue {
   repeated group WrappedAsGroupFoo Foos = 4;
}
")]
        [InlineData(typeof(ManualWrappedEquivalent), @"syntax = ""proto3"";
package ProtoBuf.Test;

message Foo {
   int32 Id = 1;
}
message ManualWrappedEquivalent {
   WrapperLayer Wrapper = 4;
}
message WrapperLayer {
   repeated Foo Foos = 1;
}
")]
        [InlineData(typeof(ManualWrappedGroupEquivalent), @"syntax = ""proto3"";
package ProtoBuf.Test;

message Foo {
   int32 Id = 1;
}
message ManualWrappedGroupEquivalent {
   group WrapperLayer Wrapper = 4;
}
message WrapperLayer {
   repeated Foo Foos = 1;
}
")]
        public void SchemaGenerationSucceeds(Type type, string expected)
        {
            var model = RuntimeTypeModel.Create();
            model.Add(type);
            var actual = model.GetSchema(type, ProtoSyntax.Proto3);
            Log.WriteLine(actual);
            if (expected is not null)
            {
                Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
            }
        }

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "22-00")]
        [InlineData(1, "22-04-0A-02-08-00")]
        [InlineData(10, "22-28-0A-02-08-00-0A-02-08-01-0A-02-08-02-0A-02-08-03-0A-02-08-04-0A-02-08-05-0A-02-08-06-0A-02-08-07-0A-02-08-08-0A-02-08-09")]
        public void TestWithNullWrappedCollection(int count, string? hex = null) => Test<WithNullWrappedCollection>(count, true, hex);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "23-24")]
        [InlineData(1, "23-0A-02-08-00-24")]
        [InlineData(10, "23-0A-02-08-00-0A-02-08-01-0A-02-08-02-0A-02-08-03-0A-02-08-04-0A-02-08-05-0A-02-08-06-0A-02-08-07-0A-02-08-08-0A-02-08-09-24")]
        public void TestWithNullWrappedGroupCollection(int count, string? hex = null) => Test<WithNullWrappedGroupCollection>(count, true, hex);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "22-00")]
        [InlineData(1, "22-06-0A-04-0A-02-08-00")]
        [InlineData(10, "22-30-0A-04-0A-02-08-00-0A-04-0A-02-08-01-0A-00-0A-04-0A-02-08-03-0A-04-0A-02-08-04-0A-00-0A-04-0A-02-08-06-0A-04-0A-02-08-07-0A-00-0A-04-0A-02-08-09")]
        public void TestWithNullWrappedCollection_WrappedValues(int count, string? hex = null) => Test<WithNullWrappedCollection_WrappedValues>(count, true, hex, true);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "23-24")]
        [InlineData(1, "23-0A-04-0A-02-08-00-24")]
        [InlineData(10, "23-0A-04-0A-02-08-00-0A-04-0A-02-08-01-0A-00-0A-04-0A-02-08-03-0A-04-0A-02-08-04-0A-00-0A-04-0A-02-08-06-0A-04-0A-02-08-07-0A-00-0A-04-0A-02-08-09-24")]
        public void TestWithNullWrappedGroupCollection_WrappedValues(int count, string? hex = null) => Test<WithNullWrappedGroupCollection_WrappedValues>(count, true, hex, true);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "")]
        [InlineData(1, "22-02-08-00")]
        [InlineData(10, "22-02-08-00-22-02-08-01-22-02-08-02-22-02-08-03-22-02-08-04-22-02-08-05-22-02-08-06-22-02-08-07-22-02-08-08-22-02-08-09")]
        public void TestVanilla(int count, string? hex = null) => Test<Vanilla>(count, false, hex);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "")]
        [InlineData(1, "22-04-0A-02-08-00")]
        [InlineData(10, "22-04-0A-02-08-00-22-04-0A-02-08-01-22-00-22-04-0A-02-08-03-22-04-0A-02-08-04-22-00-22-04-0A-02-08-06-22-04-0A-02-08-07-22-00-22-04-0A-02-08-09")]
        public void TestVanillaWrappedValue(int count, string? hex = null) => Test<VanillaWrappedValue>(count, false, hex, true);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "")]
        [InlineData(1, "23-0A-02-08-00-24")]
        [InlineData(10, "23-0A-02-08-00-24-23-0A-02-08-01-24-23-24-23-0A-02-08-03-24-23-0A-02-08-04-24-23-24-23-0A-02-08-06-24-23-0A-02-08-07-24-23-24-23-0A-02-08-09-24")]
        public void TestVanillaWrappedGroupValue(int count, string? hex = null) => Test<VanillaWrappedGroupValue>(count, false, hex, true);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "22-00")]
        [InlineData(1, "22-04-0A-02-08-00")]
        [InlineData(10, "22-28-0A-02-08-00-0A-02-08-01-0A-02-08-02-0A-02-08-03-0A-02-08-04-0A-02-08-05-0A-02-08-06-0A-02-08-07-0A-02-08-08-0A-02-08-09")]
        public void TestManualWrappedEquivalent(int count, string? hex = null) => Test<ManualWrappedEquivalent>(count, true, hex);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "23-24")]
        [InlineData(1, "23-0A-02-08-00-24")]
        [InlineData(10, "23-0A-02-08-00-0A-02-08-01-0A-02-08-02-0A-02-08-03-0A-02-08-04-0A-02-08-05-0A-02-08-06-0A-02-08-07-0A-02-08-08-0A-02-08-09-24")]
        public void TestManualWrappedGroupEquivalent(int count, string? hex = null) => Test<ManualWrappedGroupEquivalent>(count, true, hex);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "22-00")]
        [InlineData(1, "22-06-0A-04-0A-02-08-00")]
        [InlineData(10, "22-30-0A-04-0A-02-08-00-0A-04-0A-02-08-01-0A-00-0A-04-0A-02-08-03-0A-04-0A-02-08-04-0A-00-0A-04-0A-02-08-06-0A-04-0A-02-08-07-0A-00-0A-04-0A-02-08-09")]
        public void TestManualWrappedEquivalent_WrappedValues(int count, string? hex = null) => Test<ManualWrappedEquivalent_WrappedValues>(count, true, hex, true);

        [Theory]
        [InlineData(-1, "")]
        [InlineData(0, "23-24")]
        [InlineData(1, "23-0A-04-0A-02-08-00-24")]
        [InlineData(10, "23-0A-04-0A-02-08-00-0A-04-0A-02-08-01-0A-00-0A-04-0A-02-08-03-0A-04-0A-02-08-04-0A-00-0A-04-0A-02-08-06-0A-04-0A-02-08-07-0A-00-0A-04-0A-02-08-09-24")]
        public void TestManualWrappedGroupEquivalent_WrappedValues(int count, string? hex = null) => Test<ManualWrappedGroupEquivalent_WrappedValues>(count, true, hex, true);


        private void Test<T>(int count, bool preserveEmpty, string? expectedHex, bool usesWrappedValues = false, [CallerMemberName] string name = "") where T : class, ITestScenario, new()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<T>();
            Test<T>(model, count, preserveEmpty, expectedHex, true, usesWrappedValues);
            model.CompileInPlace();
            Test<T>(model, count, preserveEmpty, expectedHex, false, usesWrappedValues);
            Test<T>(count == 0 ? PEVerify.CompileAndVerify(model, name) : model.Compile(), count, preserveEmpty, expectedHex, false, usesWrappedValues);
        }

        private void Test<T>(TypeModel model, int count, bool preserveEmpty, string? expectedHex, bool logHex, bool usesWrappedValues) where T : class, ITestScenario, new()
        {
            bool ShouldBeNull(int index) => usesWrappedValues && (index % 3) == 2;
            T obj = new();
            if (count >= 0)
            {
                var list = obj.Foos = new List<Foo?>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(ShouldBeNull(i) ? null : new(i));
                }
            }
            using var ms = new MemoryStream();
            model.Serialize<T>(ms, obj);
            long streamWriterLength = ms.Length;
            if (!ms.TryGetBuffer(out var buffer)) buffer = new(ms.ToArray());
            var hex = BitConverter.ToString(buffer.Array!, buffer.Offset, buffer.Count);
            if (logHex) Log.WriteLine(hex);

            if (expectedHex is not null) Assert.Equal(expectedHex, hex);

            ms.Position = 0;
            var clone = model.Deserialize<T>(ms);

            if (count < 0 || (count == 0 && !preserveEmpty))
            {
                Assert.Null(clone.Foos);
            }
            else
            {
                var list = clone.Foos;
                Assert.NotNull(list);
                Assert.Equal(count, list.Count);
                for (int i = 0; i < count; i++)
                {
                    var listItem = list[i];
                    if (ShouldBeNull(i))
                    {
                        Assert.Null(listItem);
                    }
                    else
                    {
                        Assert.NotNull(listItem);
                        Assert.Equal(i, listItem.Id);
                    }
                }
            }

            // check that the buffer-writer API works
            ms.Position = 0;
            ms.SetLength(0);
            using var writer = new StreamCopyingBufferWriter(ms);

            model.Serialize<T>(writer, obj);
            if (!ms.TryGetBuffer(out buffer)) buffer = new(ms.ToArray());
            hex = BitConverter.ToString(buffer.Array!, buffer.Offset, buffer.Count);

            if (expectedHex is not null) Assert.Equal(expectedHex, hex);
            Assert.Equal(streamWriterLength, ms.Length);

            // check that the measure API works (null-writer)
            var nullWriterLength = model.Measure<T>(obj).LengthOnly();
            Assert.Equal(streamWriterLength, nullWriterLength);
        }

        [ProtoContract]
        public class Vanilla : ITestScenario
        {
            [ProtoMember(4)]
            public List<Foo?>? Foos { get; set; }
        }

        [ProtoContract]
        public class VanillaWrappedValue : ITestScenario
        {
            [ProtoMember(4), NullWrappedValue]
            public List<Foo?>? Foos { get; set; }
        }

        [ProtoContract]
        public class VanillaWrappedGroupValue : ITestScenario
        {
            [ProtoMember(4), NullWrappedValue(AsGroup = true)]
            public List<Foo?>? Foos { get; set; }
        }

        [ProtoContract]
        public class ManualWrappedEquivalent : ITestScenario
        {
            [ProtoContract]
            public class WrapperLayer
            {
                public WrapperLayer() => Foos = new();
                public WrapperLayer(List<Foo?> foos) => Foos = foos;

                [ProtoMember(1)]
                public List<Foo?> Foos { get; set; }
            }

            [ProtoMember(4)]
            public WrapperLayer? Wrapper {get;set;}
            List<Foo?>? ITestScenario.Foos
            {
                get => Wrapper?.Foos;
                set
                {
                    if (value is null)
                    {
                        Wrapper = null;
                    }
                    else if (Wrapper is null)
                    {
                        Wrapper = new(value);
                    }
                    else
                    {
                        Wrapper.Foos = value;
                    }
                }
            }
        }

        [ProtoContract]
        public class ManualWrappedEquivalent_WrappedValues : ITestScenario
        {
            [ProtoContract]
            public class WrapperLayer
            {
                public WrapperLayer() => Foos = new();
                public WrapperLayer(List<Foo?> foos) => Foos = foos;

                [ProtoMember(1), NullWrappedValue]
                public List<Foo?> Foos { get; set; }
            }

            [ProtoMember(4)]
            public WrapperLayer? Wrapper { get; set; }
            List<Foo?>? ITestScenario.Foos
            {
                get => Wrapper?.Foos;
                set
                {
                    if (value is null)
                    {
                        Wrapper = null;
                    }
                    else if (Wrapper is null)
                    {
                        Wrapper = new(value);
                    }
                    else
                    {
                        Wrapper.Foos = value;
                    }
                }
            }
        }

        [ProtoContract]
        public class ManualWrappedGroupEquivalent : ITestScenario
        {
            [ProtoContract]
            public class WrapperLayer
            {
                public WrapperLayer() => Foos = new();
                public WrapperLayer(List<Foo?> foos) => Foos = foos;

                [ProtoMember(1)]
                public List<Foo?> Foos { get; set; }
            }

            [ProtoMember(4, DataFormat = DataFormat.Group)]
            public WrapperLayer? Wrapper { get; set; }
            List<Foo?>? ITestScenario.Foos
            {
                get => Wrapper?.Foos;
                set
                {
                    if (value is null)
                    {
                        Wrapper = null;
                    }
                    else if (Wrapper is null)
                    {
                        Wrapper = new(value);
                    }
                    else
                    {
                        Wrapper.Foos = value;
                    }
                }
            }
        }

        [ProtoContract]
        public class ManualWrappedGroupEquivalent_WrappedValues : ITestScenario
        {
            [ProtoContract]
            public class WrapperLayer
            {
                public WrapperLayer() => Foos = new();
                public WrapperLayer(List<Foo?> foos) => Foos = foos;

                [ProtoMember(1), NullWrappedValue]
                public List<Foo?> Foos { get; set; }
            }

            [ProtoMember(4, DataFormat = DataFormat.Group)]
            public WrapperLayer? Wrapper { get; set; }
            List<Foo?>? ITestScenario.Foos
            {
                get => Wrapper?.Foos;
                set
                {
                    if (value is null)
                    {
                        Wrapper = null;
                    }
                    else if (Wrapper is null)
                    {
                        Wrapper = new(value);
                    }
                    else
                    {
                        Wrapper.Foos = value;
                    }
                }
            }
        }


        [ProtoContract]
        public class InvalidMarkedNullWrappedCollection
        {
            [ProtoMember(1), NullWrappedCollection]
            public Foo Foo { get; set; } = default!;
        }

        [ProtoContract]
        public class WithNullWrappedCollection : ITestScenario
        {
            [ProtoMember(4), NullWrappedCollection]
            public List<Foo?>? Foos { get; set; }
        }

        [ProtoContract]
        public class WithNullWrappedGroupCollection : ITestScenario
        {
            [ProtoMember(4), NullWrappedCollection(AsGroup = true)]
            public List<Foo?>? Foos { get; set; }
        }

        [ProtoContract]
        public class WithNullWrappedCollection_WrappedValues : ITestScenario
        {
            [ProtoMember(4), NullWrappedCollection, NullWrappedValue]
            public List<Foo?>? Foos { get; set; }
        }

        [ProtoContract]
        public class WithNullWrappedGroupCollection_WrappedValues : ITestScenario
        {
            [ProtoMember(4), NullWrappedCollection(AsGroup = true), NullWrappedValue]
            public List<Foo?>? Foos { get; set; }
        }

        public interface ITestScenario
        {
            List<Foo?>? Foos { get; set; }
        }

        public sealed class Foo
        {
            public Foo(int id) => Id = id; 
            public int Id { get; }
        }
    }
}
