using System;
using System.Globalization;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue222
    {
        [Test]
        public void TestNonNullableDateTimeOffsetViaSurrogate()
        {
            var foo = new Foo {X = DateTimeOffset.Now};
            var model = TypeModel.Create();
            model.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate));
            model.AutoCompile = false;
            
            var clone = (Foo)model.DeepClone(foo);
            Assert.AreEqual(foo.X, clone.X, "runtime");

            model.CompileInPlace();
            clone = (Foo)model.DeepClone(foo);
            Assert.AreEqual(foo.X, clone.X, "CompileInPlace");

            clone = (Foo)model.Compile().DeepClone(foo);
            Assert.AreEqual(foo.X, clone.X, "Compile");
        }
        [Test]
        public void TestNullableDateTimeOffsetViaSurrogate()
        {
            var bar = new Bar { X = DateTimeOffset.Now };
            var model = TypeModel.Create();
            model.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate));
            model.AutoCompile = false;

            var clone = (Bar)model.DeepClone(bar);
            Assert.AreEqual(bar.X, clone.X, "runtime");

            model.CompileInPlace();
            clone = (Bar)model.DeepClone(bar);
            Assert.AreEqual(bar.X, clone.X, "CompileInPlace");

            clone = (Bar)model.Compile().DeepClone(bar);
            Assert.AreEqual(bar.X, clone.X, "Compile");
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public DateTimeOffset X { get; set; }
        }
        [ProtoContract]
        public class Bar
        {
            [ProtoMember(1)]
            public DateTimeOffset? X { get; set; }
        }
        [ProtoContract]
        public class DateTimeOffsetSurrogate
        {
          public DateTimeOffsetSurrogate(DateTimeOffset dto)
          {
            Content = dto.ToString("o");
          }
          [ProtoMember(1)]
          public string Content { get; set; }

          public static explicit operator DateTimeOffsetSurrogate(DateTimeOffset dto)
          {
            return new DateTimeOffsetSurrogate(dto);
          }

          public static explicit operator DateTimeOffset(DateTimeOffsetSurrogate dtos)
          {
            return DateTimeOffset.Parse(dtos.Content, null, DateTimeStyles.RoundtripKind);
          }
        }
    }
}
