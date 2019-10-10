using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Issues
{
    
    public class SO18277323
    {
        [ProtoContract]
        [ProtoInclude(3, typeof(SourceTableResponse))]
        public class BaseResponse
        {
            [ProtoMember(1)]
            public bool Success { get; set; }
            [ProtoMember(2)]
            public string Error { get; set; }
        }
        [ProtoContract]
        public class SourceTableResponse : BaseResponse
        {
            [ProtoMember(1)]
            public Dictionary<string, Dictionary<string, string>> FieldValuesByTableName { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(3, typeof(CustomSourceTableResponse), DataFormat = DataFormat.Group)]
        public class CustomBaseResponse
        {
            [ProtoMember(1)]
            public bool Success { get; set; }
            [ProtoMember(2)]
            public string Error { get; set; }
        }
        [ProtoContract]
        public class CustomSourceTableResponse : CustomBaseResponse
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public List<FieldTable> FieldValuesByTableName { get { return fieldValuesByTableName; } }
            private readonly List<FieldTable> fieldValuesByTableName = new List<FieldTable>();
        }
        [ProtoContract]
        public class FieldTable
        {
            public FieldTable() { }
            public FieldTable(string tableName)
            {
                TableName = tableName;
            }
            [ProtoMember(1)]
            public string TableName { get; set; }
            [ProtoMember(2, DataFormat = DataFormat.Group)]
            public List<FieldValue> FieldValues { get { return fieldValues; } }
            private readonly List<FieldValue> fieldValues = new List<FieldValue>();
        }
        [ProtoContract]
        public class FieldValue
        {
            public FieldValue() { }
            public FieldValue(string name, string value)
            {
                Name = name;
                Value = value;
            }
            [ProtoMember(1)]
            public string Name { get; set; }
            [ProtoMember(2)]
            public string Value { get; set; }
        }


        static BaseResponse CreateSimpleObj()
        {
            return new SourceTableResponse
            {
                Success = true,
                Error = "ok",
                FieldValuesByTableName = new Dictionary<string, Dictionary<string, string>> {
                    {"abc", new Dictionary<string,string> { {"def", "ghi"}}}
                }
            };
        }
        static CustomBaseResponse CreateCustomObj()
        {
            return new CustomSourceTableResponse
            {
                Success = true,
                Error = "ok",
                FieldValuesByTableName = {
                    new FieldTable("abc") { FieldValues = { new FieldValue("def", "ghi")}}
                }
            };
        }
        private void CheckObject(BaseResponse obj)
        {
            Assert.NotNull(obj);
            Assert.IsType<SourceTableResponse>(obj);
            Assert.True(obj.Success);
            Assert.Equal("ok", obj.Error);
            SourceTableResponse typed = (SourceTableResponse)obj;
            Assert.NotNull(typed.FieldValuesByTableName);
            Assert.Single(typed.FieldValuesByTableName);
            var pair = typed.FieldValuesByTableName.Single();
            Assert.Equal("abc", pair.Key);
            var dict = pair.Value;
            Assert.NotNull(dict);
            Assert.Single(dict);
            var innerPair = dict.Single();
            Assert.Equal("def", innerPair.Key);
            Assert.Equal("ghi", innerPair.Value);
        }
        private void CheckObject(CustomBaseResponse obj)
        {
            Assert.NotNull(obj);
            Assert.IsType<CustomSourceTableResponse>(obj);
            Assert.True(obj.Success);
            Assert.Equal("ok", obj.Error);
            CustomSourceTableResponse typed = (CustomSourceTableResponse)obj;
            Assert.NotNull(typed.FieldValuesByTableName);
            Assert.Single(typed.FieldValuesByTableName);
            var pair = typed.FieldValuesByTableName.Single();
            Assert.Equal("abc", pair.TableName);
            var dict = pair.FieldValues;
            Assert.NotNull(dict);
            Assert.Single(dict);
            var innerPair = dict.Single();
            Assert.Equal("def", innerPair.Name);
            Assert.Equal("ghi", innerPair.Value);
        }
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            return model;
        }

        [Fact]
        public void ExecuteSimple()
        {
            using var ms = new MemoryStream();
            var model = CreateModel();
            model.Serialize(ms, CreateSimpleObj());
            Assert.Equal("1A-13-0A-11-0A-03-61-62-63-12-0A-0A-03-64-65-66-12-03-67-68-69-08-01-12-02-6F-6B", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
            ms.Position = 0;
#pragma warning disable CS0618
            var clone = (BaseResponse)model.Deserialize(ms, null, typeof(BaseResponse));
#pragma warning restore CS0618
            CheckObject(clone);
        }

        [Fact]
        public void ExecuteCustom()
        {
            using var ms = new MemoryStream();
            var model = CreateModel();
#if DEBUG
                model.ForwardsOnly = true;
#endif
            model.Serialize(ms, CreateCustomObj());
            Assert.Equal("1B-0B-0A-03-61-62-63-13-0A-03-64-65-66-12-03-67-68-69-14-0C-1C-08-01-12-02-6F-6B", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
            ms.Position = 0;
#pragma warning disable CS0618
            var clone = (CustomBaseResponse)model.Deserialize(ms, null, typeof(CustomBaseResponse));
#pragma warning restore CS0618
            CheckObject(clone);
        }
    }


}
