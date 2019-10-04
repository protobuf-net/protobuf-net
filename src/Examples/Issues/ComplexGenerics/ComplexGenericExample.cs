namespace Examples.Issues.ComplexGenerics
{
/* Written in response to a question about how to handle multiple "packet" subclasses;
 * may as well keep it as a test...
 * */

    using ProtoBuf;
    using System.Data;
    using Xunit;
    using System;
    using System.ComponentModel;
    using System.IO;
    using ProtoBuf.Meta;
    using Xunit.Abstractions;

    public class ComplexGenericTest
    {
        [Fact]
        public void VerifyIL()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Query));
            model.Compile("ComplexGenericTest", "ComplexGenericTest.dll");
            PEVerify.AssertValid("ComplexGenericTest.dll");
        }

        [Fact]
        public void TestX()
        {
            Query query = new X { Result = "abc" };
            Assert.Equal(typeof(string), query.GetQueryType());

            using var ms = new MemoryStream();
            Serializer.Serialize<Query>(ms, query);
            ms.Position = 0;
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("B2-01-05-0A-03-61-62-63", hex);
            // B2-01 = field 22, type String
            // 05 = length 5
            // 0A = field 1, type String
            // 03 = length 3
            // 61-62-63 = abc            
            Query clone = Serializer.Deserialize<Query>(ms);
            Assert.NotNull(clone);
            Assert.NotSame(clone, query);
            Assert.IsType(query.GetType(), clone);
            Assert.Equal(((X)query).Result, ((X)clone).Result);
        }
        [Fact]
        public void TestY()
        {
            Query query = new Y { Result = 1234};
            Assert.Equal(typeof(int), query.GetQueryType());
            Query clone = Serializer.DeepClone<Query>(query);
            Assert.NotNull(clone);
            Assert.NotSame(clone, query);
            Assert.IsType(query.GetType(), clone);
            Assert.Equal(((Y)query).Result, ((Y)clone).Result);
        }
        
    }
    public static class QueryExt {
        public static Type GetQueryType(this IQuery query)
        {
            if (query == null) throw new ArgumentNullException("query");
            foreach (Type type in query.GetType().GetInterfaces())
            {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IQuery<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }
            throw new ArgumentException("No typed query implemented", "query");
        }
    }
    public interface IQuery
    {
        string Result { get; set; }
    }
    public interface IQuery<T> : IQuery
    {
        new T Result { get; set; }
    }

    [ProtoInclude(21, typeof(W))]
    [ProtoInclude(22, typeof(X))]
    [ProtoInclude(23, typeof(Y))]
    [ProtoInclude(25, typeof(SpecialQuery))]
    [ProtoContract]
    public abstract class Query : IQuery
    {
        public string Result
        {
            get { return ResultString; }
            set { ResultString = value; }
        }
        public abstract string ResultString { get; set; }

        protected static string FormatQueryString<T>(T value)
        {
            return TypeDescriptor.GetConverter(typeof(T))
                .ConvertToInvariantString(value);
        }
        protected static T ParseQueryString<T>(string value)
        {
            return (T) TypeDescriptor.GetConverter(typeof(T))
                .ConvertFromInvariantString(value);
        }
    }
    [ProtoContract]
    [ProtoInclude(21, typeof(Z))]
    public abstract class SpecialQuery : Query, IQuery<DataSet>
    {
        
        public new DataSet Result { get; set; }

        [ProtoMember(1)]
        public override string ResultString
        {
            get {
                if (Result == null) return null;
                using (StringWriter sw = new StringWriter())
                {
                    Result.WriteXml(sw, XmlWriteMode.WriteSchema);
                    return sw.ToString();
                }
            }
            set {
                if (value == null) { Result = null; return; }
                using (StringReader sr = new StringReader(value))
                {
                    using DataSet ds = new DataSet();
                    ds.ReadXml(sr, XmlReadMode.ReadSchema);
                }
            }
        }
    }

    [ProtoContract]
    public class W : Query, IQuery<bool>
    {
        [ProtoMember(1)]
        public new bool Result { get; set; }

        public override string ResultString
        {
            get {return FormatQueryString(Result); }
            set { Result = ParseQueryString<bool>(value); }
        }
    }
    [ProtoContract]
    public class X : Query, IQuery<string>
    {
        [ProtoMember(1)]
        public new string Result { get; set; }

        public override string ResultString
        {
            get { return Result ; }
            set { Result = value; }
        }
    }
    [ProtoContract]
    public class Y : Query, IQuery<int>
    {
        [ProtoMember(1)]
        public new int Result { get; set; }

        public override string ResultString
        {
            get { return FormatQueryString(Result); }
            set { Result = ParseQueryString<int>(value); }
        }
    }
    [ProtoContract]
    public class Z : SpecialQuery
    {
    }
}