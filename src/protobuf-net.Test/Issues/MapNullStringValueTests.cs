using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// v2 handled a <see cref="Dictionary{TKey,TValue}"/> member in two ways. Registered at runtime
    /// through <see cref="MetaType.Add(int, string)"/> it was a list of key/value pairs: a null value
    /// was simply absent on the wire and came back as null. Registered through attributes it was a
    /// map, and a missing string value came back as ""; a byte[] value came back as null in both
    /// modes. At the v2 compatibility levels v3 preserves the null exactly where v2 did; Level300
    /// reads every missing value as the proto default.
    ///
    /// The expected results below are the measured behaviour of protobuf-net 2.4.9, and the wire
    /// constants are the bytes 2.4.9 wrote for the same configuration.
    /// </summary>
    public class MapNullStringValueTests
    {
        public enum Registration
        {
            RuntimeAdd,          // MetaType.Add(n, name) on a type added without default behaviour: a list in v2
            Attribute,           // [ProtoMember]: a map
            AttributeDisableMap, // [ProtoMember] with [ProtoMap(DisableMap = true)]: a list
            ImplicitFields,      // [ProtoContract(ImplicitFields = AllPublic)] without [ProtoMember]: a map
            AddWithDefaults,     // MetaType.Add(type, applyDefaultBehaviour: true) on an attributed type: a map
        }

        public class PlainStrings { public Dictionary<string, string> Map; }
        public class PlainBytes { public Dictionary<string, byte[]> Map; }

        [ProtoContract]
        public class AttributedStrings { [ProtoMember(1)] public Dictionary<string, string> Map; }
        [ProtoContract]
        public class AttributedBytes { [ProtoMember(1)] public Dictionary<string, byte[]> Map; }

        [ProtoContract]
        public class NoMapStrings { [ProtoMember(1), ProtoMap(DisableMap = true)] public Dictionary<string, string> Map; }
        [ProtoContract]
        public class NoMapBytes { [ProtoMember(1), ProtoMap(DisableMap = true)] public Dictionary<string, byte[]> Map; }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ImplicitStrings { public Dictionary<string, string> Map; }
        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ImplicitBytes { public Dictionary<string, byte[]> Map; }

        // captured with a console program that serialized { "null": null, "empty": "", "x": "x" }
        // (and the byte[] equivalents: null, empty array, { 0x78 }) with protobuf-net 2.4.9 on
        // net9.0, for each registration above; the value tag is field 2 inside each pair
        private const string V2StringsAsList = "0A-06-0A-04-6E-75-6C-6C-0A-09-0A-05-65-6D-70-74-79-12-00-0A-06-0A-01-78-12-01-78";
        private const string V2StringsAsMap = "0A-06-0A-04-6E-75-6C-6C-0A-07-0A-05-65-6D-70-74-79-0A-06-0A-01-78-12-01-78";
        private const string V2Bytes = "0A-06-0A-04-6E-75-6C-6C-0A-09-0A-05-65-6D-70-74-79-12-00-0A-06-0A-01-78-12-01-78";

        private static bool IsMapInV2(Registration registration)
            => registration is Registration.Attribute or Registration.ImplicitFields or Registration.AddWithDefaults;

        private static Type HolderType(Registration registration, bool bytes) => registration switch
        {
            Registration.RuntimeAdd => bytes ? typeof(PlainBytes) : typeof(PlainStrings),
            Registration.AttributeDisableMap => bytes ? typeof(NoMapBytes) : typeof(NoMapStrings),
            Registration.ImplicitFields => bytes ? typeof(ImplicitBytes) : typeof(ImplicitStrings),
            _ => bytes ? typeof(AttributedBytes) : typeof(AttributedStrings),
        };

        private static RuntimeTypeModel CreateModel(Registration registration, bool bytes, CompatibilityLevel level = CompatibilityLevel.NotSpecified)
        {
            var model = RuntimeTypeModel.Create();
            model.DefaultCompatibilityLevel = level;
            var type = HolderType(registration, bytes);
            switch (registration)
            {
                case Registration.RuntimeAdd:
                    // a type added without default behaviour does not take the model's default level
                    var metaType = model.Add(type, false);
                    metaType.CompatibilityLevel = level;
                    metaType.Add(1, "Map");
                    break;
                case Registration.AddWithDefaults:
                    model.Add(type, true);
                    break;
            }
            return model;
        }

        private static object CreateHolder(Type type, bool bytes)
        {
            var holder = Activator.CreateInstance(type);
            object map = bytes
                ? new Dictionary<string, byte[]> { { "null", null }, { "empty", Array.Empty<byte>() }, { "x", new byte[] { 0x78 } } }
                : new Dictionary<string, string> { { "null", null }, { "empty", "" }, { "x", "x" } };
            type.GetField("Map").SetValue(holder, map);
            return holder;
        }

        private static System.Collections.IDictionary ReadMap(RuntimeTypeModel model, Type type, byte[] payload)
        {
            using var ms = new MemoryStream(payload);
            var holder = model.Deserialize(ms, null, type);
            return (System.Collections.IDictionary)type.GetField("Map").GetValue(holder);
        }

        private static byte[] Write(RuntimeTypeModel model, object holder)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, holder);
            return ms.ToArray();
        }

        private static byte[] FromHex(string hex)
        {
            var parts = hex.Split('-');
            var result = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++) result[i] = Convert.ToByte(parts[i], 16);
            return result;
        }

        private static void AssertLikeV2(System.Collections.IDictionary map, Registration registration, bool bytes)
        {
            bool nullPreserved = bytes || !IsMapInV2(registration);
            if (bytes)
            {
                if (nullPreserved) Assert.Null(map["null"]);
                else Assert.Empty((byte[])map["null"]);
                Assert.Empty((byte[])map["empty"]);
                Assert.Equal(new byte[] { 0x78 }, (byte[])map["x"]);
            }
            else
            {
                if (nullPreserved) Assert.Null(map["null"]);
                else Assert.Equal("", map["null"]);
                Assert.Equal("", map["empty"]);
                Assert.Equal("x", map["x"]);
            }
        }

        [Theory]
        [InlineData(Registration.RuntimeAdd, false)]
        [InlineData(Registration.RuntimeAdd, true)]
        [InlineData(Registration.Attribute, false)]
        [InlineData(Registration.Attribute, true)]
        [InlineData(Registration.AttributeDisableMap, false)]
        [InlineData(Registration.AttributeDisableMap, true)]
        [InlineData(Registration.ImplicitFields, false)]
        [InlineData(Registration.ImplicitFields, true)]
        [InlineData(Registration.AddWithDefaults, false)]
        [InlineData(Registration.AddWithDefaults, true)]
        public void IsMap_MatchesV2(Registration registration, bool bytes)
        {
            var model = CreateModel(registration, bytes);
            Assert.Equal(IsMapInV2(registration), model[HolderType(registration, bytes)][1].IsMap);
        }

        [Theory]
        [InlineData(Registration.RuntimeAdd, false)]
        [InlineData(Registration.RuntimeAdd, true)]
        [InlineData(Registration.Attribute, false)]
        [InlineData(Registration.Attribute, true)]
        [InlineData(Registration.AttributeDisableMap, false)]
        [InlineData(Registration.AttributeDisableMap, true)]
        [InlineData(Registration.ImplicitFields, false)]
        [InlineData(Registration.ImplicitFields, true)]
        [InlineData(Registration.AddWithDefaults, false)]
        [InlineData(Registration.AddWithDefaults, true)]
        public void RoundTrip_ReadsLikeV2(Registration registration, bool bytes)
        {
            var type = HolderType(registration, bytes);
            foreach (var level in new[] { CompatibilityLevel.NotSpecified, CompatibilityLevel.Level200, CompatibilityLevel.Level240 })
            {
                var model = CreateModel(registration, bytes, level);
                var map = ReadMap(model, type, Write(model, CreateHolder(type, bytes)));
                AssertLikeV2(map, registration, bytes);
            }
        }

        [Theory]
        [InlineData(Registration.RuntimeAdd, false)]
        [InlineData(Registration.RuntimeAdd, true)]
        [InlineData(Registration.Attribute, false)]
        [InlineData(Registration.Attribute, true)]
        [InlineData(Registration.AttributeDisableMap, false)]
        [InlineData(Registration.AttributeDisableMap, true)]
        [InlineData(Registration.ImplicitFields, false)]
        [InlineData(Registration.ImplicitFields, true)]
        [InlineData(Registration.AddWithDefaults, false)]
        [InlineData(Registration.AddWithDefaults, true)]
        public void V2Wire_ReadsLikeV2(Registration registration, bool bytes)
        {
            var type = HolderType(registration, bytes);
            var wire = bytes ? V2Bytes : IsMapInV2(registration) ? V2StringsAsMap : V2StringsAsList;
            var map = ReadMap(CreateModel(registration, bytes), type, FromHex(wire));
            AssertLikeV2(map, registration, bytes);
        }

        [Theory]
        [InlineData(Registration.RuntimeAdd, false)]
        [InlineData(Registration.RuntimeAdd, true)]
        [InlineData(Registration.Attribute, false)]
        [InlineData(Registration.Attribute, true)]
        [InlineData(Registration.AttributeDisableMap, false)]
        [InlineData(Registration.AttributeDisableMap, true)]
        [InlineData(Registration.ImplicitFields, false)]
        [InlineData(Registration.ImplicitFields, true)]
        [InlineData(Registration.AddWithDefaults, false)]
        [InlineData(Registration.AddWithDefaults, true)]
        public void Level300_CoercesNullToEmpty(Registration registration, bool bytes)
        {
            // proto3 semantics: a missing value is the default value, for every registration
            var type = HolderType(registration, bytes);
            var model = CreateModel(registration, bytes, CompatibilityLevel.Level300);
            var map = ReadMap(model, type, Write(model, CreateHolder(type, bytes)));
            if (bytes)
            {
                Assert.Empty((byte[])map["null"]);
                Assert.Empty((byte[])map["empty"]);
            }
            else
            {
                Assert.Equal("", map["null"]);
                Assert.Equal("", map["empty"]);
            }
        }

        [Fact]
        public void Writer_DistinguishesNullFromEmpty_AtAllLevels()
        {
            // null omits the value tag, "" writes it with length 0: the read side can tell them apart
            foreach (var level in new[] { CompatibilityLevel.Level200, CompatibilityLevel.Level240, CompatibilityLevel.Level300 })
            {
                var model = CreateModel(Registration.RuntimeAdd, false, level);
                var withNull = new PlainStrings { Map = new Dictionary<string, string> { { "k", null } } };
                var withEmpty = new PlainStrings { Map = new Dictionary<string, string> { { "k", "" } } };
                Assert.Equal("0A-03-0A-01-6B", BitConverter.ToString(Write(model, withNull)));
                Assert.Equal("0A-05-0A-01-6B-12-00", BitConverter.ToString(Write(model, withEmpty)));
            }
        }

        [Fact]
        public void Level300Reader_CoercesWireFromLevel200()
        {
            // a null written at Level200 (value tag absent) is read as "" under Level300
            var writer = CreateModel(Registration.RuntimeAdd, false, CompatibilityLevel.Level200);
            var payload = Write(writer, CreateHolder(typeof(PlainStrings), false));
            var reader = CreateModel(Registration.RuntimeAdd, false, CompatibilityLevel.Level300);
            var map = ReadMap(reader, typeof(PlainStrings), payload);
            Assert.Equal("", map["null"]);
            Assert.Equal("", map["empty"]);
        }

        [Fact]
        public void ExplicitSupportNull_PreservesNullRegardlessOfLevel()
        {
            // SupportNull uses the wrapped-value path, which preserves null at every level
            foreach (var level in new[] { CompatibilityLevel.Level200, CompatibilityLevel.Level240, CompatibilityLevel.Level300 })
            {
                var model = RuntimeTypeModel.Create();
                model.DefaultCompatibilityLevel = level;
                var metaType = model.Add(typeof(PlainStrings), false);
                metaType.Add(1, nameof(PlainStrings.Map));
                metaType[1].SupportNull = true;
                var map = ReadMap(model, typeof(PlainStrings), Write(model, CreateHolder(typeof(PlainStrings), false)));
                Assert.Null(map["null"]);
                Assert.Equal("", map["empty"]);
                Assert.Equal("x", map["x"]);
            }
        }
    }
}
