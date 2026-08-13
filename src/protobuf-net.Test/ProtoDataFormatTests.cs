using ProtoBuf;
using ProtoBuf.Internal;
using System;
using Xunit;

namespace ProtoBuf.Test
{
    public class ProtoDataFormatTests
    {
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        [ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
        public class Declaring { }

        public class Derived : Declaring { }

        [ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
        public class Overriding : Declaring { }

        public class Plain { }

        [Fact]
        public void DeclaredTypeIsMatched()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(Guid)));

        [Fact]
        public void MultipleDeclarationsAreKeyedByType()
            => Assert.Equal(DataFormat.ZigZag,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(int)));

        [Fact]
        public void UndeclaredTypeGetsDefault()
            => Assert.Equal(DataFormat.Default,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(long)));

        [Fact]
        public void BaseTypeDeclarationIsInherited()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Derived), typeof(Guid)));

        [Fact]
        public void DerivedDeclarationWinsOverBase()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Overriding), typeof(int)));

        [Fact]
        public void UndecoratedTypeGetsDefault()
            => Assert.Equal(DataFormat.Default,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Plain), typeof(Guid)));
    }
}
