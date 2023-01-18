using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Reflection.Test
{
    public class DecoderRecordExample
    {
        private readonly ITestOutputHelper _log;
        public DecoderRecordExample(ITestOutputHelper log) => _log = log;

        [Fact]
        public void CanParseData()
        {
            var schema = GetDummySchema();
            using var payload = GetDummyPayload();

            var visitor = new RecordVisitor();
            var obj = (IDataRecord)visitor.Visit(payload, schema, "Test");

            var sb = new StringBuilder();
            for (int i = 0; i < obj.FieldCount; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(obj.GetName(i)).Append(" ").Append(obj.GetDataTypeName(i));
            }
            var s = sb.ToString();
            Assert.Equal("a Int32, b List`1, c Record, d Int32, e Int32, f List`1, g Int32, h Int32", s);

            Assert.Equal(8, obj.FieldCount);
            Assert.Equal(150, obj.GetInt32(obj.GetOrdinal("a")));
            Assert.Equal("testing", ((List<string>)obj["b"]).Single());
            var c = (IDataRecord)obj["c"];
            Assert.NotNull(c);
            Assert.Equal(150, c.GetInt32(c.GetOrdinal("a")));
            Assert.Equal(1, obj.GetInt32(obj.GetOrdinal("d")));
            Assert.Equal(5, obj.GetInt32(obj.GetOrdinal("e")));
            Assert.Equal(0, obj.GetInt32(obj.GetOrdinal("g")));
            Assert.True(obj.IsDBNull(obj.GetOrdinal("h")));

        }

        private FileDescriptorSet GetDummySchema() => GetSchema(@"
syntax=""proto3"";
message Test {
  optional int32 a = 1 [json_name=""ja""];
  repeated string b = 2 [json_name=""jb""];
  optional Test c = 3; // deliberately no json_name
  optional Blap d = 4 [json_name=""jd""];
  optional Blap e = 5; // deliberately no json_name
  repeated int32 f = 6 [packed=true, json_name=""jf""];
  int32 g = 7; // not presence-tracked; should apply default value (never specified)
  optional int32 h = 8; // presence-tracked; should *NOT* apply default value (never specified)
}
enum Blap {
   BLAB_X = 0;
   BLAB_Y = 1;
}");

        private FileDescriptorSet GetSchema(string source)
        {
            var schemaSet = new FileDescriptorSet();
            // inspired from the encoding document
            schemaSet.Add("dummy.proto", source: new StringReader(source));
            schemaSet.Process();
            foreach (var error in schemaSet.GetErrors())
            {
                _log.WriteLine(error.Message);
            }
            return schemaSet;
        }

        private static MemoryStream GetDummyPayload(byte fCount = 0)
        {
            var ms = new MemoryStream();
            var buffer = new byte[] {
                0x08, 0x96, 0x01, // integer
                0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67, // string
                0x1a, 0x03, 0x08, 0x96, 0x01, // sub-message
                0x20, 0x01, // enum mapped to defined value
                0x28, 0x05, // enum without defined value
            };
            ms.Write(buffer, 0, buffer.Length);
            if (fCount > 0)
            {
                // lazy; keep everything small so we can use single-byte logic
                if (fCount > 100) throw new ArgumentOutOfRangeException(nameof(fCount));
                ms.WriteByte(0x32); // field 6, length-prefixed
                ms.WriteByte(fCount); // single-byte, so: length==this value
                for (byte i = 0; i < fCount; i++)
                {
                    ms.WriteByte(i);
                }
            }
            ms.Position = 0;
            return ms;
        }
    }

    public class RecordVisitor : ObjectDecodeVisitor
    {
        private readonly Dictionary<DescriptorProto, (string name, Type type)[]> _fieldDefinitions = new();
        protected override object CreateMessageObject(in VisitContext context, FieldDescriptorProto field)
        {
            var type = context.MessageType;
            if (!_fieldDefinitions.TryGetValue(type, out var fields))
            {
                if (type.Fields.Count == 0)
                {
                    fields = Array.Empty<(string name, Type type)>();
                }
                else
                {
                    fields = new (string name, Type type)[type.Fields.Count];
                    int i = 0;
                    foreach (var defined in type.Fields)
                    {
                        fields[i++] = (defined.Name, GetType(defined));
                    }
                }
                _fieldDefinitions.Add(type, fields);
            }
            return new Record(fields);
        }

        protected override void Store<T>(in VisitContext ctx, FieldDescriptorProto field, T value, Func<T, object> box)
        {
            if (ctx.Index < 0 && ctx.Current is Record record)
            {
                record[field.Name] = box(value);
                return;
            }
            // use default collection etc handling
            base.Store(ctx, field, value, box);
        }
        protected override bool TryGetObject(in VisitContext context, FieldDescriptorProto field, out object existing)
        {
            if (context.Current is Record record)
            {
                existing = record[field.Name];
                return existing is not null;
            }
            existing = default;
            return false;
        }

        private Type GetType(FieldDescriptorProto field)
        {
            Type type = field.type switch
            {
                FieldDescriptorProto.Type.TypeMessage or FieldDescriptorProto.Type.TypeGroup => typeof(Record),
                _ => GetSuggestedType(field),
            };
            if (field.label == FieldDescriptorProto.Label.LabelRepeated)
            {
                type = typeof(List<>).MakeGenericType(type);
            }

            return type;
        }
    }

    class Record : IDataRecord
    {
        private readonly (string name, Type type)[] _fields;
        private readonly object[] _values;
        public Record((string name, Type type)[] fields)
        {
            _fields = fields;
            _values = fields.Length == 0 ? Array.Empty<object>() : new object[fields.Length];
        }
        
        public object this[int i] => _values[i];

        public object this[string name]
        {
            get => _values [GetOrdinal(name)];
            internal set => _values[GetOrdinal(name)] = value;
        } 

        public int GetOrdinal(string name)
        {
            var arr = _fields;
            for (int i = 0; i < arr.Length;i++ )
            {
                if (arr[i].name == name) return i;
            }
            return -1;
        }

        public int FieldCount => _values.Length;

        bool IDataRecord.GetBoolean(int i) => (bool)_values[i];

        byte IDataRecord.GetByte(int i) => (byte)_values[i];

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferffset, int length) => throw new NotSupportedException();

        char IDataRecord.GetChar(int i) => (char)_values[i];

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotSupportedException();

        IDataReader IDataRecord.GetData(int i) => throw new NotSupportedException();

        string IDataRecord.GetDataTypeName(int i) => _fields[i].type.Name;

        DateTime IDataRecord.GetDateTime(int i) => (DateTime)_values[i];

        decimal IDataRecord.GetDecimal(int i) => (decimal)_values[i];

        double IDataRecord.GetDouble(int i) => (double)_values[i];

        Type IDataRecord.GetFieldType(int i) => _fields[i].type;

        float IDataRecord.GetFloat(int i) => (float)_values[i];

        Guid IDataRecord.GetGuid(int i) => (Guid)_values[i];

        short IDataRecord.GetInt16(int i) => (short)_values[i];

        int IDataRecord.GetInt32(int i) => (int)_values[i];

        long IDataRecord.GetInt64(int i) => (long)_values[i];

        string IDataRecord.GetName(int i) => _fields[i].name;

        string IDataRecord.GetString(int i) => (string) _values[i];

        object IDataRecord.GetValue(int i) => _values[i];

        int IDataRecord.GetValues(object[] values)
        {
            _values.CopyTo(values, 0);
            return _values.Length;
        }

        public bool IsDBNull(int i) => _values[i] is null or DBNull;
    }
}
