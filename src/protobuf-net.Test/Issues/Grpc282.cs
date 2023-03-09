using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    // https://github.com/protobuf-net/protobuf-net.Grpc/issues/282
    public class Grpc282
    {
        static TypeModel CreateModel<T>(int mode, [CallerMemberName] string name = "")
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<T>();
            switch (mode)
            {
                case 0:
                    return model;
                case 1:
                    model.CompileInPlace();
                    return model;
                case 2:
                    return PEVerify.CompileAndVerify(model, name);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        static T RoundTrip<T>(TypeModel model, T value, string expectedHex)
        {
            using var ms = new MemoryStream();
            model.Serialize<T>(ms, value);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new(ms.ToArray());
            var hex = BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
            Assert.Equal(expectedHex, hex);

            ms.Position = 0;
            return model.Deserialize<T>(ms);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ExecuteSimple(int mode)
        {
            var model = CreateModel<setCommCellIdRequest>(mode);
            var clone = RoundTrip(model, new setCommCellIdRequest
            {
                m_pEvAlertObj = 42,
            }, "10-2A"); // Field #2 Varint Value = 42,
            Assert.Equal(42, clone.m_pEvAlertObj);
        }

        [Theory]
        [InlineData(typeof(setCommCellIdRequest), @"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

message setCommCellIdRequest {
   int64 commCellId = 1;
   int64 m_pEvAlertObj = 2;
   bool isDisposed = 3;
}
")]
        [InlineData(typeof(WithDefaults), @"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

message WithDefaults {
   int64 A = 1; // default value could not be applied: 134
   int64 B = 2;
   uint64 C = 3; // default value could not be applied: 136
   uint64 D = 4;
}
")]
        [InlineData(typeof(WithDefaults), @"syntax = ""proto2"";
package ProtoBuf.Test.Issues;

message WithDefaults {
   optional int64 A = 1 [default = 134];
   optional int64 B = 2;
   optional uint64 C = 3 [default = 136];
   optional uint64 D = 4;
}
", ProtoSyntax.Proto2)]
        public void TestSchema(Type type, string expected, ProtoSyntax syntax = ProtoSyntax.Proto3)
            => Assert.Equal(expected, RuntimeTypeModel.Create().GetSchema(type, syntax), ignoreLineEndingDifferences: true);

        [ProtoContract]
        public class setCommCellIdRequest
        {
            [ProtoMember(1)]
            public Int64 commCellId { get; set; }

            [ProtoMember(2)]
            public nint m_pEvAlertObj { get; set; }

            [ProtoMember(3)]
            public Boolean isDisposed { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ExecuteWithDefaults(int mode)
        {
            var model = CreateModel<WithDefaults>(mode);
            var clone = RoundTrip(model, new WithDefaults
            {
                A = 134, C = 136,
            }, "");
            Assert.Equal(134, clone.A);
            Assert.Null(clone.B);
            Assert.Equal(136U, clone.C);
            Assert.Null(clone.D);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ExecuteWithNonDefaults(int mode)
        {
            var model = CreateModel<WithDefaults>(mode);
            var clone = RoundTrip(model, new WithDefaults
            {
                A = 1,
                B = 2,
                C = 3,
                D = 4,
            }, "08-01-10-02-18-03-20-04"); // 1/2/3/4 as varints
            Assert.Equal(1, clone.A);
            Assert.Equal(2, clone.B);
            Assert.Equal(3U, clone.C);
            Assert.Equal(4U, clone.D);
        }

        [ProtoContract]
        public class WithDefaults
        {
            [ProtoMember(1)]
            [DefaultValue(134)]
            public nint A { get; set; } = 134;

            [ProtoMember(2)]
            public nint? B { get; set; }

            [ProtoMember(3)]
            [DefaultValue(136)]
            public nuint C { get; set; } = 136;

            [ProtoMember(4)]
            public nuint? D { get; set; }
        }
    }
}
