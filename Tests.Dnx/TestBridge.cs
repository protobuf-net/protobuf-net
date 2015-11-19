using System;
using ProtoBuf.Meta;
using ProtoBuf.unittest.Meta;
#if NO_INTERNAL_CONTEXT
namespace ProtoBuf.unittest.Serializers
{
    static partial class Util
    {

    }
}
#endif

#if XUNIT


namespace NUnit.Framework
{
    public class TestFixtureAttribute : Attribute { }

    public class TestAttribute : Xunit.FactAttribute { }

    // note: this doesn't *work*; it requires code changes
    public class ExpectedExceptionAttribute : Attribute {
        public ExpectedExceptionAttribute(Type type) { }
    }
    static class Assert
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
        public static void IsNull(object @object, string message = null)
        {
            Xunit.Assert.Null(@object);
        }
        public static void IsNotNull(object @object, string message = null)
        {
            Xunit.Assert.NotNull(@object);
        }

        internal static void AreNotSame(object expected, object actual, string message = null)
        {
            Xunit.Assert.Same(expected, actual);
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
            Xunit.Assert.True(bigger > smaller);
        }
        internal static void Greater(int bigger, int smaller, string message = null)
        {
            Xunit.Assert.True(bigger > smaller);
        }
    }
}
#endif