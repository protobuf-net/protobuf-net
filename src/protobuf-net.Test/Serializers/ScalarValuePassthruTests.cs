using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using ProtoBuf.Meta;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.unittest.Serializers
{
    public sealed class ScalarValuePassthruTests
    {
        private readonly ITestOutputHelper _tw;

        public ScalarValuePassthruTests(ITestOutputHelper tw)
        {
            _tw = tw;
        }

        private byte[] SerializeToArray(object obj, RuntimeTypeModel model)
        {
            var ms = new MemoryStream();
            model.Serialize(ms, obj);
            Assert.NotEqual(0, ms.Length);
            return ms.ToArray();
        }

        private static RuntimeTypeModel CreateModelWithIDTypeConfigured()
        {
            var model = TypeModel.Create();
            model.Add(typeof(CustomerID), false).ScalarValuePassthru = true;
            return model;
        }

        private static RuntimeTypeModel CreateModelWithScalarValuePassthruInference()
        {
            var model = TypeModel.Create();
            Assert.False(model.InferScalarValuePassthru);
            model.InferScalarValuePassthru = true;
            return model;
        }

        private static RuntimeTypeModel CreatePristineModel()
        {
            return TypeModel.Create();
        }

        private static T DeepClone<T>(T original, RuntimeTypeModel model)
        {
            var clone = model.DeepClone(original);
            return (T)clone;
        }


        [ProtoContract]
        public sealed class I64_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public CustomerID CustomerID;
        }

        [ProtoContract]
        public sealed class I64Raw_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public long StudentID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_Roundtrip(bool autoCompile)
        {
            var orig = new I64_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            };
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var clone = DeepClone(orig, model);
            Assert.Equal(10, clone.ID);
            Assert.Equal(444, clone.CustomerID.ValueNoThrow);
        }

        // This type has a shape that is recognizable to AutoTuple detection:
        // Readonly props that are matched by a ctor.
        public struct DetectScalarValuePassthru_ID
        {
            public long Value { get; private set; }

            public DetectScalarValuePassthru_ID(long value) => Value = value;
        }

        [ProtoContract]
        public sealed class DetectScalarValuePassthru_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public DetectScalarValuePassthru_ID OtherID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DetectScalarValuePassthru(bool autoCompile)
        {
            // Scalar value passthru inference makes some kinds of structs serialize
            // with scalar value passthru that would otherwise be serialized
            // by TupleSerializer as a nested structure.
            // To avoid that kind of breakage, this test ensures tries to make sure
            // that you only get it when you ask for it.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;

            {
                // with passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                model.InferScalarValuePassthru = true;
                bytesWithPassthruInference = SerializeToArray(new DetectScalarValuePassthru_Message
                {
                    ID = 10,
                    OtherID = new DetectScalarValuePassthru_ID(444),
                }, model);
                Assert.True(model[typeof(CustomerID)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesWithoutPassthruInference = SerializeToArray(new DetectScalarValuePassthru_Message
                {
                    ID = 10,
                    OtherID = new DetectScalarValuePassthru_ID(444),
                }, model);
                Assert.False(model[typeof(CustomerID)].ScalarValuePassthru);
            }
            Assert.NotEqual(bytesWithPassthruInference, bytesWithoutPassthruInference);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_Bytes(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var bytesFromTyped = SerializeToArray(new I64_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            }, model);
            var bytesFromRaw = SerializeToArray(new I64Raw_Message
            {
                ID = 10,
                StudentID = 444,
            }, CreatePristineModel());
            Assert.Equal(bytesFromRaw, bytesFromTyped);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_DefaultBytes_DefaultIsNotOmitted(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            // The default value (when specified or derived) is not written.
            // Ideally it would behave the same with passthru, but to keep these changes
            // minimally intrusive to protobuf-net, default values are not derived or handled.
            // That difference in behavior is probably not so bad, since nullable value types
            // are thing, and are a usually a better expression of missing values than
            // default/zero values are.
            var bytesFromTyped = SerializeToArray(new I64_Message
            {
                ID = 10,
                CustomerID = default(CustomerID),
            }, model);
            var bytesFromRaw = SerializeToArray(new I64Raw_Message
            {
                ID = 10,
                StudentID = 0,
            }, CreatePristineModel());
            Assert.NotEqual(bytesFromRaw, bytesFromTyped);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_RoundtripDefaultNot0(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model[typeof(I64Raw_Message)][1].DefaultValue = 444L;
            model.AutoCompile = autoCompile;

            //			model[typeof(I64_Message)][1].DefaultValue = 444;
            var bytesFromTyped = SerializeToArray(new I64_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            }, model);

            var bytesFromRaw = SerializeToArray(new I64Raw_Message
            {
                ID = 10,
                StudentID = 444,
            }, model);
            Assert.Equal(bytesFromRaw, bytesFromTyped);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_RoundtripDefault(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var clone = DeepClone(new I64_Message
            {
                ID = 10,
                CustomerID = default(CustomerID),
            }, model);
            Assert.Equal(default(CustomerID), clone.CustomerID);
        }

        [Fact]
        public void I64_Schema()
        {
            var orig = new I64_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            };
            var model = CreateModelWithIDTypeConfigured();
            var schema = model.GetSchema(orig.GetType());

            _tw.WriteLine("schema:");
            _tw.WriteLine(schema);
            // Note the absense of a message named CustomerID, and the use of int64 instead of CustomerID for the CustomerID field.
            // Also, no default value is specified, in contrast to the plain in64 ID field.
            // That is not by design, rather it is to keep the required changes to protobuf-net minimal.
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.unittest.Serializers;

message I64_Message {
   optional int64 ID = 1 [default = 0];
   optional int64 CustomerID = 10;
}
", schema);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.unittest.Serializers;

message I64_Message {
   int64 ID = 1;
   int64 CustomerID = 10;
}
", model.GetSchema(orig.GetType(), ProtoSyntax.Proto3));
        }

        [ProtoContract]
        public sealed class I64Prop_Message
        {
            [ProtoMember(1)] public long ID { get; set; }

            [ProtoMember(10)] public CustomerID CustomerID { get; set; }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64Prop_Roundtrip(bool autoCompile)
        {
            // most of the other tests are using fields. Using properties should behave the same.
            // We do not replicate all of the test with variations with properties, though;
            // instead we trust that the system handles them the same.
            // So we'll just have this small test.
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var clone = DeepClone(new I64Prop_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            }, model);
            Assert.Equal(10, clone.ID);
            Assert.Equal(444, clone.CustomerID.ValueNoThrow);
        }


        [ProtoContract]
        public sealed class I64_Nullable_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public CustomerID? CustomerID;
        }

        [ProtoContract]
        public sealed class I64Raw_Nullable_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public long? StudentID;
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_Roundtrip_Nullable(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            // Using non-null value.

            var clone = DeepClone(new I64_Nullable_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            }, model);
            Assert.Equal(10, clone.ID);
            Assert.Equal((CustomerID)444, clone.CustomerID);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_RoundtripDefault_Nullable(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            {
                // Roundtrip null zero/default value.
                var i64clone = DeepClone(new I64_Nullable_Message
                {
                    ID = 10,
                    CustomerID = null,
                }, model);
                var i64rawclone = DeepClone(new I64Raw_Nullable_Message
                {
                    ID = 10,
                    StudentID = null,
                }, CreatePristineModel());
                Assert.Null(i64clone.CustomerID);
                Assert.Null(i64rawclone.StudentID);
            }
            {
                // Roundtrip non-null zero/default value.
                var i64clone = DeepClone(new I64_Nullable_Message
                {
                    ID = 10,
                    CustomerID = default(CustomerID),
                }, model);
                var i64rawclone = DeepClone(new I64Raw_Nullable_Message
                {
                    ID = 10,
                    StudentID = default(long),
                }, CreatePristineModel());
                Assert.Equal(default(CustomerID), i64clone.CustomerID);
                Assert.Equal(default(long), i64rawclone.StudentID);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_Bytes_Nullable(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            {
                // Using non-null value.
                var bytesFromTyped = SerializeToArray(new I64_Nullable_Message
                {
                    ID = 10,
                    CustomerID = (CustomerID)444,
                }, model);
                var bytesFromRaw = SerializeToArray(new I64Raw_Nullable_Message
                {
                    ID = 10,
                    StudentID = 444,
                }, CreatePristineModel());
                Assert.Equal(bytesFromRaw, bytesFromTyped);
            }
            {
                // Using null value.
                var bytesFromTyped = SerializeToArray(new I64_Nullable_Message
                {
                    ID = 10,
                    CustomerID = null,
                }, model);
                var bytesFromRaw = SerializeToArray(new I64Raw_Nullable_Message
                {
                    ID = 10,
                    StudentID = null,
                }, CreatePristineModel());
                Assert.Equal(bytesFromRaw, bytesFromTyped);
            }
        }

        [Fact]
        public void I64_Schema_Nullable()
        {
            var orig = new I64_Nullable_Message
            {
                ID = 10,
                CustomerID = (CustomerID)444,
            };
            var model = CreateModelWithIDTypeConfigured();

            var schema = model.GetSchema(orig.GetType());
            _tw.WriteLine("schema:");
            _tw.WriteLine(schema);
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.unittest.Serializers;

message I64_Nullable_Message {
   optional int64 ID = 1 [default = 0];
   optional int64 CustomerID = 10;
}
", schema);
        }


        [ProtoContract]
        public sealed class I64_List_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public CustomerID[] CustomerIDs;
        }

        [ProtoContract]
        public sealed class I64Raw_List_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public long[] StudentIDs;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_Roundtrip_List(bool autoCompile)
        {
            var orig = new I64_List_Message
            {
                ID = 10,
                CustomerIDs = new[] { (CustomerID)444, (CustomerID)555 },
            };
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var clone = DeepClone(orig, model);
            Assert.Equal(10, clone.ID);
            Assert.Equal(2, clone.CustomerIDs.Length);
            Assert.Equal((CustomerID)444, clone.CustomerIDs[0]);
            Assert.Equal((CustomerID)555, clone.CustomerIDs[1]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_RoundtripDefault_List(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            // Clone raw longs first, to establish it's behavior.
            var rawclone = DeepClone(new I64Raw_List_Message
            {
                ID = 10,
                StudentIDs = new[] { 444L, default(long) },
            }, CreatePristineModel());
            Assert.Equal(2, rawclone.StudentIDs.Length);
            Assert.Equal(444, rawclone.StudentIDs[0]);
            Assert.Equal(default(long), rawclone.StudentIDs[1]);


            // Now check that we get the same results.

            var clone = DeepClone(new I64_List_Message
            {
                ID = 10,
                CustomerIDs = new[] { (CustomerID)444, default(CustomerID) },
            }, model);
            Assert.Equal(10, clone.ID);
            Assert.Equal(2, clone.CustomerIDs.Length);
            Assert.Equal((CustomerID)444, clone.CustomerIDs[0]);
            Assert.Equal(default(CustomerID), clone.CustomerIDs[1]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_Bytes_List(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var bytesFromTyped = SerializeToArray(new I64_List_Message
            {
                ID = 10,
                CustomerIDs = new[] { (CustomerID)444, (CustomerID)555 },
            }, model);
            var bytesFromRaw = SerializeToArray(new I64Raw_List_Message
            {
                ID = 10,
                StudentIDs = new[] { 444L, 555L },
            }, CreatePristineModel());
            Assert.Equal(bytesFromRaw, bytesFromTyped);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I64_BytesDefault_List(bool autoCompile)
        {
            var model = CreateModelWithIDTypeConfigured();
            model.AutoCompile = autoCompile;

            var bytesFromTyped = SerializeToArray(new I64_List_Message
            {
                ID = 10,
                CustomerIDs = new[] { (CustomerID)444, default(CustomerID) },
            }, model);
            var bytesFromRaw = SerializeToArray(new I64Raw_List_Message
            {
                ID = 10,
                StudentIDs = new[] { 444L, default(long) },
            }, CreatePristineModel());
            Assert.Equal(bytesFromRaw, bytesFromTyped);
        }

        [Fact]
        public void I64_Schema_List()
        {
            var orig = new I64_List_Message
            {
                ID = 10,
                CustomerIDs = new[] { (CustomerID)444, (CustomerID)555 },
            };
            var model = CreateModelWithIDTypeConfigured();
            var schema = model.GetSchema(orig.GetType());

            _tw.WriteLine("schema:");
            _tw.WriteLine(schema);
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.unittest.Serializers;

message I64_List_Message {
   optional int64 ID = 1 [default = 0];
   repeated int64 CustomerIDs = 10;
}
", schema);
        }

        [Fact]
        public void I64Raw_Schema_List()
        {
            // This test is mainly here to know/document the schema that is generated for plain int64s.
            var orig = new I64Raw_List_Message
            {
                ID = 10,
                StudentIDs = new[] { 444L, 555L },
            };
            var model = CreateModelWithIDTypeConfigured();
            var schema = model.GetSchema(orig.GetType());

            _tw.WriteLine("schema:");
            _tw.WriteLine(schema);
        }

        public struct I32_ID
        {
            public int Value { get; private set; }

            public I32_ID(int value) => Value = value;
        }

        [ProtoContract]
        public sealed class I32_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public I32_ID OtherID;
        }

        [ProtoContract]
        public sealed class I32Raw_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public int OtherID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I32_AllInOne(bool autoCompile)
        {
            // most of the other tests are using int64. Since the impl doesn't make a distinction betweeen
            // different raw types, other raw types should behave the same.
            // So we'll just have this small test of int32 to show that it probably also works for all
            // the other raw types. Exept for string, for which we also have a test, since it is
            // a reference type.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;
            byte[] bytesForRawI32;

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                bytesWithPassthruInference = SerializeToArray(new I32_Message
                {
                    ID = 10,
                    OtherID = new I32_ID(444),
                }, model);
                Assert.True(model[typeof(I32_ID)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesWithoutPassthruInference = SerializeToArray(new I32_Message
                {
                    ID = 10,
                    OtherID = new I32_ID(444),
                }, model);
                Assert.False(model[typeof(I32_ID)].ScalarValuePassthru);
            }
            {
                // raw i32.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesForRawI32 = SerializeToArray(new I32Raw_Message
                {
                    ID = 10,
                    OtherID = 444,
                }, model);
            }

            var clone = DeepClone(new I32_Message
            {
                ID = 10,
                OtherID = new I32_ID(444),
            }, CreateModelWithScalarValuePassthruInference());
            Assert.Equal(444, clone.OtherID.Value);

            Assert.Equal(bytesForRawI32, bytesWithPassthruInference);
            Assert.NotEqual(bytesWithPassthruInference, bytesWithoutPassthruInference);
        }

        public struct I32N_ID
        {
            public int? Value { get; private set; }

            public I32N_ID(int value) => Value = value;
        }

        [ProtoContract]
        public sealed class I32N_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public I32N_ID OtherID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void I32N_NotSupported(bool autoCompile)
        {
            // The purpose of scalar value passthru is to wrap "primitive" types.
            // It is not obvious that it makes sense to wrap a nullable primitive type.
            // So for now at least, this test ensures that we do not commit to supporting that.

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                var ex = Assert.Throws<InvalidOperationException>(() => SerializeToArray(new I32N_Message
                {
                    ID = 10,
                    OtherID = new I32N_ID(444),
                }, model));
                Assert.Contains("No serializer defined for type:", ex.Message);
            }
        }

        public struct String_ID
        {
            public string Value { get; private set; }

            public String_ID(string value) => Value = value;
        }

        [ProtoContract]
        public sealed class String_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public String_ID OtherID;
        }

        [ProtoContract]
        public sealed class StringRaw_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public string OtherID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void String_AllInOne(bool autoCompile)
        {
            // See I32_AllInOne.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;
            byte[] bytesForRawString;

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                bytesWithPassthruInference = SerializeToArray(new String_Message
                {
                    ID = 10,
                    OtherID = new String_ID("x1"),
                }, model);
                Assert.True(model[typeof(String_ID)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesWithoutPassthruInference = SerializeToArray(new String_Message
                {
                    ID = 10,
                    OtherID = new String_ID("x1"),
                }, model);
                Assert.False(model[typeof(String_ID)].ScalarValuePassthru);
            }
            {
                // raw i32.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesForRawString = SerializeToArray(new StringRaw_Message
                {
                    ID = 10,
                    OtherID = "x1",
                }, model);
            }

            var clone = DeepClone(new String_Message
            {
                ID = 10,
                OtherID = new String_ID("x1"),
            }, CreateModelWithScalarValuePassthruInference());
            Assert.Equal("x1", clone.OtherID.Value);
            _tw.WriteLine("Serialized base64: " + Convert.ToBase64String(bytesWithPassthruInference));
            Assert.Equal(bytesForRawString, bytesWithPassthruInference);
            Assert.NotEqual(bytesWithPassthruInference, bytesWithoutPassthruInference);
        }

        [ProtoContract]
        public struct ProtoContract_ID
        {
            [ProtoMember(1)]
            public int Value { get; private set; }

            public ProtoContract_ID(int value) => Value = value;
        }

        [ProtoContract]
        public sealed class ProtoContract_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public ProtoContract_ID OtherID;
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ProtoContract_AllInOne(bool autoCompile)
        {
            // We want to check the effect of applying ProtoContract and ProtoMember to
            // a type that could otherwise look like a wrapper type:
            // In that case we should just do what we have  "always" done with such a type: Treat it as a classic contract.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;
            byte[] bytesWithExplicitPassthru;
            byte[] bytesForRawI32;

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                bytesWithPassthruInference = SerializeToArray(new ProtoContract_Message
                {
                    ID = 10,
                    OtherID = new ProtoContract_ID(444),
                }, model);
                Assert.False(model[typeof(ProtoContract_ID)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesWithoutPassthruInference = SerializeToArray(new ProtoContract_Message
                {
                    ID = 10,
                    OtherID = new ProtoContract_ID(444),
                }, model);
                Assert.False(model[typeof(ProtoContract_ID)].ScalarValuePassthru);
            }
            {
                // with passthru for the type explicitly enabled: What ought to take precedence?
                // At least this piece of code documents the current behavior.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                model[typeof(ProtoContract_ID)].ScalarValuePassthru = true;
                bytesWithExplicitPassthru = SerializeToArray(new ProtoContract_Message
                {
                    ID = 10,
                    OtherID = new ProtoContract_ID(444),
                }, model);
            }
            {
                // raw i32.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesForRawI32 = SerializeToArray(new I32Raw_Message
                {
                    ID = 10,
                    OtherID = 444,
                }, model);
            }

            var clone = DeepClone(new I32_Message
            {
                ID = 10,
                OtherID = new I32_ID(444),
            }, CreateModelWithScalarValuePassthruInference());
            Assert.Equal(444, clone.OtherID.Value);

            Assert.NotEqual(bytesForRawI32, bytesWithPassthruInference);
            Assert.Equal(bytesWithPassthruInference, bytesWithoutPassthruInference);
            Assert.Equal(bytesForRawI32, bytesWithExplicitPassthru);
        }

        [ProtoContract]
        public struct DataContract_ID
        {
            [DataMember]
            public int Value { get; private set; }

            public DataContract_ID(int value) => Value = value;
        }

        [ProtoContract]
        public sealed class DataContract_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public DataContract_ID OtherID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DataContract_AllInOne(bool autoCompile)
        {
            // We want to check the effect of applying ProtoContract and ProtoMember to
            // a type that could otherwise look like a wrapper type:
            // In that case we should just do what we have  "always" done with such a type: Treat it as a classic contract.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;
            byte[] bytesWithExplicitPassthru;
            byte[] bytesForRawI32;

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                bytesWithPassthruInference = SerializeToArray(new DataContract_Message
                {
                    ID = 10,
                    OtherID = new DataContract_ID(444),
                }, model);
                Assert.False(model[typeof(DataContract_ID)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesWithoutPassthruInference = SerializeToArray(new DataContract_Message
                {
                    ID = 10,
                    OtherID = new DataContract_ID(444),
                }, model);
                Assert.False(model[typeof(DataContract_ID)].ScalarValuePassthru);
            }
            {
                // with passthru for the type explicitly enabled: What ought to take precedence?
                // At least this piece of code documents the current behavior.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                model[typeof(DataContract_ID)].ScalarValuePassthru = true;
                bytesWithExplicitPassthru = SerializeToArray(new DataContract_Message
                {
                    ID = 10,
                    OtherID = new DataContract_ID(444),
                }, model);
            }
            {
                // raw i32.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesForRawI32 = SerializeToArray(new I32Raw_Message
                {
                    ID = 10,
                    OtherID = 444,
                }, model);
            }

            var clone = DeepClone(new I32_Message
            {
                ID = 10,
                OtherID = new I32_ID(444),
            }, CreateModelWithScalarValuePassthruInference());
            Assert.Equal(444, clone.OtherID.Value);

            Assert.NotEqual(bytesForRawI32, bytesWithPassthruInference);
            Assert.Equal(bytesWithPassthruInference, bytesWithoutPassthruInference);
            Assert.Equal(bytesForRawI32, bytesWithExplicitPassthru);
        }

        public struct Surrogate_ID_TheSurrogate
        {
            public int Value { get; private set; }

            public Surrogate_ID_TheSurrogate(int value) => Value = value;

            public static explicit operator Surrogate_ID_TheSurrogate(Surrogate_Using value)
            {
                return new Surrogate_ID_TheSurrogate(value.Field1 * 100 + value.Field2);
            }

            public static explicit operator Surrogate_Using(Surrogate_ID_TheSurrogate value)
            {
                return new Surrogate_Using(value.Value / 100, value.Value % 100);
            }
        }

        [ProtoContract(Surrogate = typeof(Surrogate_ID_TheSurrogate))]
        public struct Surrogate_Using
        {
            public int Field1 { get; private set; }
            public int Field2 { get; private set; }

            public Surrogate_Using(int field1, int field2)
            {
                Field1 = field1;
                Field2 = field2;
            }
        }

        [ProtoContract]
        public sealed class Surrogate_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public Surrogate_Using OtherUsing;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Surrogate_AllInOne(bool autoCompile)
        {
            // We would like for surrogate types to also be passthru serializable.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;
            byte[] bytesForRawI32;

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                bytesWithPassthruInference = SerializeToArray(new Surrogate_Message
                {
                    ID = 10,
                    OtherUsing = new Surrogate_Using(5, 7),
                }, model);
                Assert.False(model[typeof(Surrogate_Using)].ScalarValuePassthru);
                Assert.True(model[typeof(Surrogate_ID_TheSurrogate)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesWithoutPassthruInference = SerializeToArray(new Surrogate_Message
                {
                    ID = 10,
                    OtherUsing = new Surrogate_Using(5, 7),
                }, model);
                Assert.False(model[typeof(Surrogate_Using)].ScalarValuePassthru);
                Assert.False(model[typeof(Surrogate_ID_TheSurrogate)].ScalarValuePassthru);
            }
            {
                // raw i32, which is what we want the surrogate to serialize as.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesForRawI32 = SerializeToArray(new I32Raw_Message
                {
                    ID = 10,
                    OtherID = 5 * 100 + 7,
                }, model);
            }

            var clone = DeepClone(new Surrogate_Message
            {
                ID = 10,
                OtherUsing = new Surrogate_Using(5, 7),
            }, CreateModelWithScalarValuePassthruInference());
            Assert.Equal(5, clone.OtherUsing.Field1);
            Assert.Equal(7, clone.OtherUsing.Field2);

            _tw.WriteLine("Serialized base64, surr: " + Convert.ToBase64String(bytesWithPassthruInference));
            _tw.WriteLine("Serialized base64, i32raw: " + Convert.ToBase64String(bytesForRawI32));

            Assert.Equal(bytesForRawI32, bytesWithPassthruInference);
            Assert.NotEqual(bytesWithPassthruInference, bytesWithoutPassthruInference);

            _tw.WriteLine("schema:");
            var schema = CreateModelWithScalarValuePassthruInference().GetSchema(typeof(Surrogate_Message));
            _tw.WriteLine(schema);
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.unittest.Serializers;

message Surrogate_Message {
   optional int64 ID = 1 [default = 0];
   optional int32 OtherUsing = 2;
}
", schema);

        }

        [ProtoContract]
        public sealed class MapKeyRaw_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public Dictionary<long, string> TheMap;
        }
        [ProtoContract]
        public sealed class MapKey_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public Dictionary<CustomerID, string> TheMap;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MapKey_NotImplemented(bool autoCompile)
        {
            // This test just makes sure that some kind of exception happen when using scalar value passthru
            // for map keys, until such use might be supported.
            // It seems that supporting it is mainly a question of accepting such types as keys,
            // maybe checking that they implement iequatable, and finally making sure that
            // ScalarValuePassthruDecorator is being used.

            byte[] bytesWithPassthruInference;
            byte[] bytesWithoutPassthruInference;
            byte[] bytesForRawI64;

            {
                // with passthru inference.
                var model = CreateModelWithScalarValuePassthruInference();
                model.AutoCompile = autoCompile;

                Action testCode = () =>
                {
                    bytesWithPassthruInference = SerializeToArray(new MapKey_Message
                    {
                        ID = 10,
                        TheMap = new Dictionary<CustomerID, string> { { (CustomerID)42, "hey" } }
                    }, model);
                };
                if (autoCompile)
                {
                    var ex = Assert.Throws<InvalidOperationException>(testCode);
                    Assert.Contains(" was not possible to prepare a serializer for:", ex.Message);
                }
                else
                {
                    var ex = Assert.Throws<NotSupportedException>(testCode);
                }
                // Assert.False(model[typeof(Surrogate_Using)].ScalarValuePassthru);
                // Assert.True(model[typeof(Surrogate_ID_TheSurrogate)].ScalarValuePassthru);
            }
            {
                // without passthru inference.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    bytesWithoutPassthruInference = SerializeToArray(new MapKey_Message
                    {
                        ID = 10,
                        TheMap = new Dictionary<CustomerID, string> { { (CustomerID)42, "hey" } }
                    }, model);
                });
                Assert.Contains("No serializer defined for type:", ex.Message);
                // Assert.False(model[typeof(Surrogate_Using)].ScalarValuePassthru);
                // Assert.False(model[typeof(Surrogate_ID_TheSurrogate)].ScalarValuePassthru);
            }
            {
                // raw i64, which is what we want the surrogate to serialize as.
                var model = CreatePristineModel();
                model.AutoCompile = autoCompile;

                bytesForRawI64 = SerializeToArray(new MapKeyRaw_Message
                {
                    ID = 10,
                    TheMap = new Dictionary<long, string> { { 42, "hey" } }
                }, model);
            }

            //var clone = DeepClone(new Surrogate_Message
            //{
            //    ID = 10,
            //    OtherUsing = new Surrogate_Using(5, 7),
            //}, CreateModelWithScalarValuePassthruInference());
            //Assert.Equal(5, clone.OtherUsing.Field1);
            //Assert.Equal(7, clone.OtherUsing.Field2);

            //_tw.WriteLine("Serialized base64, surr: " + Convert.ToBase64String(bytesWithPassthruInference));
            //_tw.WriteLine("Serialized base64, i32raw: " + Convert.ToBase64String(bytesForRawI64));

            //Assert.Equal(bytesForRawI64, bytesWithPassthruInference);
            //Assert.NotEqual(bytesWithPassthruInference, bytesWithoutPassthruInference);

            //_tw.WriteLine("schema:");
            //var schema = CreateModelWithScalarValuePassthruInference().GetSchema(typeof(Surrogate_Message));
            //_tw.WriteLine(schema);
            //Assert.Equal(@"syntax = ""proto2"";
            //package ProtoBuf.unittest.Serializers;
            
            //message Surrogate_Message {
            //   optional int64 ID = 1 [default = 0];
            //   optional int32 OtherUsing = 2;
            //}
            //", schema);

        }


        public struct UnsupportedPassthruType_ID
        {
            public I64_Message Value { get; private set; }

            public UnsupportedPassthruType_ID(I64_Message value) => Value = value;
        }

        [ProtoContract]
        public sealed class UnsupportedPassthruType_Message
        {
            [ProtoMember(1)] public long ID;
            [ProtoMember(2)] public UnsupportedPassthruType_ID OtherID;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnsupportedPassthruType_AllInOne(bool autoCompile)
        {
            // Scalar value passthru was made for types that wrap primitive types.
            // This test is here to poke into what happens when we encounter a type
            // that wraps something else.
            // In this test it wraps a "real" message type, and the wrapper type 
            // will get ValueTuple handling, but that is kind of besides the point.

            var model = CreateModelWithScalarValuePassthruInference();
            model.AutoCompile = autoCompile;

            SerializeToArray(new UnsupportedPassthruType_Message
            {
                ID = 10,
                OtherID = new UnsupportedPassthruType_ID(new I64_Message { ID = 10 }),
            }, model);
            Assert.False(model[typeof(UnsupportedPassthruType_ID)].ScalarValuePassthru);
        }


        [DebuggerDisplay("{Value,nq}")]
        public struct CustomerID : IEquatable<CustomerID>
        {
            private long _value;

            public CustomerID(long value) => _value = value;

            public long Value => _value != default ? _value : throw new InvalidOperationException("ID has no value. Does this value come from an unitialized field?");
            public long ValueNoThrow => _value;

            public override string ToString() => Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);

            public override int GetHashCode() => _value.GetHashCode();

            public bool Equals(CustomerID other) => other._value == this._value;
            public override bool Equals(object obj) => obj is CustomerID v && Equals(v);

            public static bool operator ==(CustomerID x, CustomerID y) => x.Equals(y);
            public static bool operator !=(CustomerID x, CustomerID y) => !x.Equals(y);

            public static explicit operator CustomerID(long value) => new CustomerID(value);
        }
    }
}