using NUnit.Framework;
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
    [TestFixture]
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
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType(typeof(SourceTableResponse), obj);
            Assert.IsTrue(obj.Success);
            Assert.AreEqual("ok", obj.Error);
            SourceTableResponse typed = (SourceTableResponse)obj;
            Assert.IsNotNull(typed.FieldValuesByTableName);
            Assert.AreEqual(1, typed.FieldValuesByTableName.Count);
            var pair = typed.FieldValuesByTableName.Single();
            Assert.AreEqual("abc", pair.Key);
            var dict = pair.Value;
            Assert.IsNotNull(dict);
            Assert.AreEqual(1, dict.Count);
            var innerPair = dict.Single();
            Assert.AreEqual("def", innerPair.Key);
            Assert.AreEqual("ghi", innerPair.Value);
        }
        private void CheckObject(CustomBaseResponse obj)
        {
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType(typeof(CustomSourceTableResponse), obj);
            Assert.IsTrue(obj.Success);
            Assert.AreEqual("ok", obj.Error);
            CustomSourceTableResponse typed = (CustomSourceTableResponse)obj;
            Assert.IsNotNull(typed.FieldValuesByTableName);
            Assert.AreEqual(1, typed.FieldValuesByTableName.Count);
            var pair = typed.FieldValuesByTableName.Single();
            Assert.AreEqual("abc", pair.TableName);
            var dict = pair.FieldValues;
            Assert.IsNotNull(dict);
            Assert.AreEqual(1, dict.Count);
            var innerPair = dict.Single();
            Assert.AreEqual("def", innerPair.Name);
            Assert.AreEqual("ghi", innerPair.Value);
        }
        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            return model;
        }
        [Test]
        public void ExecuteSimple()
        {
            using (var ms = new MemoryStream())
            {
                var model = CreateModel();
                model.Serialize(ms, CreateSimpleObj());
                Assert.AreEqual("1A-13-0A-11-0A-03-61-62-63-12-0A-0A-03-64-65-66-12-03-67-68-69-08-01-12-02-6F-6B", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
                ms.Position = 0;
                var clone = (BaseResponse)model.Deserialize(ms, null, typeof(BaseResponse));
                CheckObject(clone);
            }
        }

        [Test]
        public void ExecuteCustom()
        {
            using (var ms = new MemoryStream())
            {
                var model = CreateModel();
                model.ForwardsOnly = true;
                model.Serialize(ms, CreateCustomObj());
                Assert.AreEqual("1B-0B-0A-03-61-62-63-13-0A-03-64-65-66-12-03-67-68-69-14-0C-1C-08-01-12-02-6F-6B", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
                ms.Position = 0;
                var clone = (CustomBaseResponse)model.Deserialize(ms, null, typeof(CustomBaseResponse));
                CheckObject(clone);
            }
        }
    }


}
