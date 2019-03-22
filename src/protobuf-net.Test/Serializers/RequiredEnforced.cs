using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace ProtoBuf.Serializers
{
	public class RequiredEnforced
	{
		[ProtoContract]
		public class HazEnforcedRequired
		{
			[ProtoMember(1, IsRequired = true)]
			public int Value
			{
				get
				{
					if(__pbn__Value == null) throw new InvalidOperationException("Field 'Value' not specified.");
					return __pbn__Value.Value;
				}
				set { __pbn__Value = value; }
			}
			public bool ShouldSerializeValue() => __pbn__Value != null;
			public void ResetValue() => __pbn__Value = null;
			private int? __pbn__Value;

			[ProtoMember(2, IsRequired = true, OverwriteList = true)]
			public byte[] BytesValue
			{
				get
				{
					if(__pbn__BytesValue == null) throw new InvalidOperationException("Field 'BytesValue' not specified.");
					return __pbn__BytesValue;
				}
				set { __pbn__BytesValue = value; }
			}
			public bool ShouldSerializeBytesValue() => __pbn__BytesValue != null;
			public void ResetBytesValue() => __pbn__BytesValue = null;
			private byte[] __pbn__BytesValue;
		}

		[Fact]
		public void HazEnforcedRequired_EnforceGet()
		{
			var source = new HazEnforcedRequired();
			Assert.Throws<InvalidOperationException>(() => source.Value);
			Assert.Throws<InvalidOperationException>(() => source.BytesValue);
			source.Value = 42;
			source.BytesValue = new byte[4];
			var x = source.Value;
			var y = source.BytesValue;
		}

		[Fact]
		public void HazEnforcedRequired_Serialize()
		{
			var ms = new MemoryStream();
			var source = new HazEnforcedRequired { Value = 42, BytesValue = new byte[4], };
			Assert.True(source.ShouldSerializeValue());
			Assert.True(source.ShouldSerializeBytesValue());
			Serializer.Serialize(ms, source);
			ms.Position = 0;
			var obj = Serializer.Deserialize<HazEnforcedRequired>(ms);
			Assert.Equal(42, obj.Value);
			Assert.True(obj.ShouldSerializeValue());
			Assert.True(obj.ShouldSerializeBytesValue());
		}
	}
}
