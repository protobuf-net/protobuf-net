namespace Examples.Issues.ComplexGenerics
{
/* Written in response to a question about how to handle multiple "packet" subclasses;
 * may as well keep it as a test...
 * */

    using ProtoBuf;
    using System.Data;
    using NUnit.Framework;
    using System;
    using System.ComponentModel;
    using System.IO;

    [TestFixture]
    public class ComplexGenericTest
    {
        [Test]
        public void TestX()
        {
            Query query = new X { Result = "abc" };
            Assert.AreEqual(typeof(string), query.GetQueryType());
            Query clone = Serializer.DeepClone<Query>(query);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(clone, query);
            Assert.IsInstanceOfType(query.GetType(), clone);
            Assert.AreEqual(((X)query).Result, ((X)clone).Result);
        }
        [Test]
        public void TestY()
        {
            Query query = new Y { Result = 1234};
            Assert.AreEqual(typeof(int), query.GetQueryType());
            Query clone = Serializer.DeepClone<Query>(query);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(clone, query);
            Assert.IsInstanceOfType(query.GetType(), clone);
            Assert.AreEqual(((Y)query).Result, ((Y)clone).Result);
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
    abstract class Query : IQuery
    {
        public string Result
        {
            get { return ResultString; }
            set { ResultString = value; }
        }
        protected abstract string ResultString { get; set; }

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
    abstract class SpecialQuery : Query, IQuery<DataSet>
    {
        
        public new DataSet Result { get; set; }

        [ProtoMember(1)]
        protected override string ResultString
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
                    DataSet ds = new DataSet();
                    ds.ReadXml(sr, XmlReadMode.ReadSchema);
                }
            }
        }
    }

    [ProtoContract]
    class W : Query, IQuery<bool>
    {
        [ProtoMember(1)]
        public new bool Result { get; set; }

        protected override string ResultString
        {
            get {return FormatQueryString(Result); }
            set { Result = ParseQueryString<bool>(value); }
        }
    }
    [ProtoContract]
    class X : Query, IQuery<string>
    {
        [ProtoMember(1)]
        public new string Result { get; set; }

        protected override string ResultString
        {
            get { return Result ; }
            set { Result = value; }
        }
    }
    [ProtoContract]
    class Y : Query, IQuery<int>
    {
        [ProtoMember(1)]
        public new int Result { get; set; }

        protected override string ResultString
        {
            get { return FormatQueryString(Result); }
            set { Result = ParseQueryString<int>(value); }
        }
    }
    [ProtoContract]
    class Z : SpecialQuery
    {
    }
}
