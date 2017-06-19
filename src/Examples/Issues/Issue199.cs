using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System.IO;

namespace Examples.Issues
{
    
    public class Issue199
    {
        [ProtoContract]
        public class DodgyDefault
        {
            public DodgyDefault()
            {
                Value = true;
            }
            [ProtoMember(1)]
            public bool Value { get; set; }
        }

        [Fact]
        public void CompareWithWithoutImplicitDefaults()
        {
            var with = TypeModel.Create();
            var without = TypeModel.Create();
            without.AutoCompile = with.AutoCompile = false;
            with.UseImplicitZeroDefaults = true;
            without.UseImplicitZeroDefaults = false;
                        

            Test(with, without, "Runtime");
            with.CompileInPlace();
            without.CompileInPlace();
            Test(with, without, "CompileInPlace");
            Test(with.Compile(), without.Compile(), "Compile");
        }

        [Fact]
        public void TryToDisableDefualtsOnDefault()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                RuntimeTypeModel.Default.UseImplicitZeroDefaults = false;
            });
        }
        [Fact]
        public void CanEnalbeDefualtsOnDefault()
        {
            RuntimeTypeModel.Default.UseImplicitZeroDefaults = true;
            Assert.True(RuntimeTypeModel.Default.UseImplicitZeroDefaults);
        }
        static void Test(TypeModel with, TypeModel without, string message)
        {
            var obj = new DodgyDefault { Value = false };

            DodgyDefault c1 = (DodgyDefault)with.DeepClone(obj);
            Assert.True(c1.Value, message);
            DodgyDefault c2 = (DodgyDefault)without.DeepClone(obj);
            Assert.False(c2.Value, message);

            using (var ms = new MemoryStream())
            {
                with.Serialize(ms, obj);
                Assert.Equal(0, ms.Length); //, message);
            }
            using (var ms = new MemoryStream())
            {
                without.Serialize(ms, obj);
                Assert.Equal(2, ms.Length); //, message);
            }
        }
    }
}
