using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Issues
{
    /// <summary>
    /// Scenario: 
    /// Proto definition is used as a contract between two different parties and definition is provided and maintained by data origin.
    /// Enum is part of the contract, however origin may need to introduce additional enum values.
    /// In reality this can happen intentionally before updating contract (for testing "in the wild") or 
    /// receiving party may fail to update software on their side based on the updated contract.
    /// In any case that would brake existing functionality at the receiving party.
    /// 
    /// Currently there are two options to deal with this:
    /// 
    /// - Explicitly define all enums in code, which would fail if contract introduces new enums over time.
    /// - Use reflection at the startup to enumerate all enums in the contract. Not very comfortable at least.
    /// 
    /// This: introduces global flag to allow enum passthru in general
    /// 
    /// </summary>
    public class UndefinedEnumValueInContract
    {
        // Origin party introduces new 4th value in the contract
        private byte[] InvalidValue = new byte[] {0x08, 0x03};

        #region "Could be automatically generated from proto contract."
        /// <summary>
        /// Enum as from contract defines 3 values.
        /// </summary>
        [ProtoContract(Name = @"TestEnum")]
        public enum TestEnum
        {
            [ProtoEnum(Name = @"V_0", Value = 0)]
            V_0 = 0,
            [ProtoEnum(Name = @"V_1", Value = 1)]
            V_1 = 1,
            [ProtoEnum(Name = @"V_2", Value = 2)]
            V_2 = 2
        }
        [ProtoContract(Name = @"Container")]
        public class Container
        {
            [ProtoMember(1, IsRequired = true, Name = @"EnumValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
            public TestEnum EnumValue { get; set; }
        }
        #endregion

        /// <summary>
        /// Receiving party chooses to fail, if value received is undefined in contract.
        /// This is the default (current) behaviour.
        /// </summary>
        [Fact]
        public void FailsWhenDeserializingUndefinedEnumValue()
        {
            var model = RuntimeTypeModel.Create();
            model.GlobalEnumPassthru = false;
            using (var ms = new MemoryStream(InvalidValue))
            {
                
                Assert.Throws<ProtoException>(() =>
                    {
                        model.Deserialize(ms, null, typeof(Container));
                    });
            }
        }

        /// <summary>
        /// Receiving party chooses globally to accept unknown values from wire
        /// </summary>
        [Fact]
        public void SucceedsWhenDeserializingUndefinedEnumValue()
        {
            var model = RuntimeTypeModel.Create();
            model.GlobalEnumPassthru = true;
            using (var ms = new MemoryStream(InvalidValue))
            {
                var obj = (Container)model.Deserialize(ms, null, typeof(Container));
                Assert.NotNull(obj);
                Assert.IsType<Container>(obj);
                Assert.Equal((TestEnum)3, obj.EnumValue);
            }
        }
    }
}
