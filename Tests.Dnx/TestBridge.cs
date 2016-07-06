//#define SINGLETEST
using System;
using ProtoBuf.Meta;
using System.Collections.Generic;

#if XUNITBRIDGE
namespace System
{
    namespace ServiceModel
    {
        class Dummy { }
    }
    namespace Runtime.Serialization.Formatters.Binary
    {
        class Dummy { }
    }
#if COREFX
    namespace ComponentModel
    {
        public class BrowsableAttribute : Attribute
        {
            public BrowsableAttribute(bool x) { }
        }
    }
    public class SerializableAttribute : Attribute { }
    public class NonSerializedAttribute : Attribute { }

    namespace IO
    {
        public static class TestFakeExtensions
        {
            public static void Close(this Stream s) { }

            public static byte[] GetBuffer(this MemoryStream ms)
            {
                ArraySegment<byte> tmp;
                if (!ms.TryGetBuffer(out tmp)) throw new InvalidOperationException("Unable to get buffer");
                return tmp.Array;
            }
        }
    }

    namespace Data.Linq
    {
        public enum ChangeAction
        {

        }
    }
    namespace Data.Linq.Mapping
    {
        public class EntitySet<T> : List<T> { }
        public class AssociationAttribute : Attribute
        {
            public bool IsForeignKey { get; set; }
            public string Name { get; set; }
            public string OtherKey { get; set; }
            public string Storage { get; set; }
            public string ThisKey { get; set; }
        }
        public class TableAttribute : Attribute
        {
            public string Name { get; set; }
        }
        public class ColumnAttribute : Attribute
        {
            public AutoSync AutoSync { get; set; }
            public string DbType { get; set; }
            public bool IsDbGenerated { get; set; }
            public bool IsPrimaryKey { get; set; }
            public string Storage { get; set; }
            public UpdateCheck UpdateCheck { get; set; }
        }
        public enum UpdateCheck
        {
            Never
        }
        public enum AutoSync
        {
            OnInsert
        }
    }
#endif
}

namespace ProtoBuf
{
    namespace ServiceModel
    {
        class Dummy { }
    }
    public static class TestFakeExtensions
    {
        public static TypeModel Compile(this RuntimeTypeModel model, string x, string y)
        {
            return model.Compile(); // use an in-memory compile instead
        }
    }
}
#endif

#if XUNIT
namespace NUnit.Framework
{
    namespace SyntaxHelpers
    {
        class Dummy { }
    }
    public class TestFixtureAttribute : Attribute { }
#if SINGLETEST
    public class ActiveTestAttribute : Xunit.FactAttribute { }
    public class TestAttribute : Attribute
    {
            public string Skip { get; set; }
    }
#else
    [Obsolete("add: #define SINGLETEST")]
    public class ActiveTestAttribute : Attribute { }
    public class TestAttribute : Xunit.FactAttribute { }
#endif
    public class IgnoreTestAttribute : TestAttribute
    {
        public IgnoreTestAttribute(string message)
        {
            Skip = string.IsNullOrWhiteSpace(message) ? "reasons" : message;
        }
    }

    // note: this doesn't *work*; it requires code changes
    [Obsolete("Requires code change")]
    public class ExpectedExceptionAttribute : Attribute
    {
        public ExpectedExceptionAttribute(Type type = null) { }
        public string ExpectedMessage { get; set; }
        public MessageMatch MatchType { get; set; }
        
    }
    public enum MessageMatch
    {
        Contains
    }
    [Obsolete("Requires code change")]
    public class IgnoreAttribute : Attribute
    {
        public IgnoreAttribute(string message = null) { }
    }

    public static class CollectionAssert
    {
        internal static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            Xunit.Assert.Equal<T>(expected, actual);
        }
    }
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            Xunit.Assert.True(condition);
        }
        internal static void IsFalse(bool condition, string message = null)
        {
            Xunit.Assert.False(condition);
        }
        public static void AreEqual<T>(T x, T y, string message = null)
        {
            Xunit.Assert.Equal<T>(x, y);
        }
        public static void AreNotEqual<T>(T x, T y, string message = null)
        {
            Xunit.Assert.NotEqual<T>(x, y);
        }
        public static void IsNull(object @object, string message = null)
        {
            Xunit.Assert.Null(@object);
        }
        public static void IsNotNull(object @object, string message = null)
        {
            Xunit.Assert.NotNull(@object);
        }
        internal static void AreSame(object expected, object actual, string message = null)
        {
            Xunit.Assert.Same(expected, actual);
        }
        internal static void AreNotSame(object expected, object actual, string message = null)
        {
            Xunit.Assert.NotSame(expected, actual);
        }

        internal static void Fail(string message = null)
        {
            Xunit.Assert.Equal("pass", "fail");
        }

        internal static void IsInstanceOfType(Type type, object instance, string message = null)
        {
            Xunit.Assert.IsType(type, instance);
        }

        internal static void Greater(long bigger, long smaller, string message = null)
        {
            Xunit.Assert.True(bigger >= smaller);
        }
        internal static void Greater(int bigger, int smaller, string message = null)
        {
            Xunit.Assert.True(bigger >= smaller);
        }
        internal static void GreaterOrEqual(long bigger, long smaller, string message = null)
        {
            Xunit.Assert.True(bigger > smaller);
        }
        internal static void GreaterOrEqual(int bigger, int smaller, string message = null)
        {
            Xunit.Assert.True(bigger > smaller);
        }
        internal static void Less(long smaller, long bigger, string message = null)
        {
            Xunit.Assert.True(smaller < bigger);
        }
        internal static void Less(int smaller, int bigger, string message = null)
        {
            Xunit.Assert.True(smaller < bigger);
        }
        internal static void LessOrEqual(long smaller, long bigger, string message = null)
        {
            Xunit.Assert.True(smaller <= bigger);
        }
        internal static void LessOrEqual(int smaller, int bigger, string message = null)
        {
            Xunit.Assert.True(smaller <= bigger);
        }
    }
}
#endif