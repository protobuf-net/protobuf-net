using Xunit;
using ProtoBuf;
using System.Linq;
using System;
namespace Examples.Issues
{
    
    public class Issue170
    {

        [Fact]
        public void ArrayWithoutNullContentShouldClone()
        {
            var arr = new[] { "aaa","bbb" };
            Assert.True(Serializer.DeepClone(arr).SequenceEqual(arr));
        }
        [Fact]
        public void ArrayWithNullContentShouldThrow()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var arr = new[] { "aaa", null, "bbb" };
                var arr2 = Serializer.DeepClone(arr);
            });
        }
    }
}
