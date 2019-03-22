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
		[ProtoContract(EnforceRequired = true)]
		public class HazEnforcedRequired
		{
			[ProtoMember(72, IsRequired = true)]
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

			[ProtoMember(15, IsRequired = true, OverwriteList = true)]
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

			[ProtoMember(3, IsRequired = false)]
			public int IntValue { get; set; }

			[ProtoMember(4, IsRequired = false)]
			public int IntValue2 { get => __pbn__IntValue2 ?? 0; set => __pbn__IntValue2 = value; }
			public bool ShouldSerializeIntValue2() => __pbn__IntValue2 != null;
			public void ResetIntValue2() => __pbn__IntValue2 = null;
			private int? __pbn__IntValue2;
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

		[ProtoContract(EnforceRequired = true)]
		public class HazEnforcedRequired_Old
		{
		}

		[ProtoContract(EnforceRequired = true)]
		public class HazEnforcedRequired_Old2
		{
			[ProtoMember(72, IsRequired = true)]
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
		}

		[Fact]
		public void HazEnforcedRequired_EnforceWrite()
		{
			var ms = new MemoryStream();
			var source = new HazEnforcedRequired { };
			Assert.False(source.ShouldSerializeValue());
			var exn = Assert.Throws<ProtoException>(() => { Serializer.Serialize(ms, source); });
			Assert.Equal("missing fields [15, 72]", exn.Message);

			source.Value = 42;
			exn = Assert.Throws<ProtoException>(() => { Serializer.Serialize(ms, source); });
			Assert.Equal("missing fields [15]", exn.Message);

			source.BytesValue = new byte[4];
			Serializer.Serialize(ms, source);
		}

		[Fact]
		public void HazEnforcedRequired_EnforceRead()
		{
			var ms = new MemoryStream();
			var source = new HazEnforcedRequired_Old { };
			Serializer.Serialize(ms, source);
			ms.Position = 0;
			var exn = Assert.Throws<ProtoException>(() => Serializer.Deserialize<HazEnforcedRequired>(ms));
			Assert.Equal("missing fields [15, 72]", exn.Message);
		}

		[Fact]
		public void HazEnforcedRequired_EnforceRead2()
		{
			var ms = new MemoryStream();
			var source = new HazEnforcedRequired_Old2 { Value = 1 };
			Serializer.Serialize(ms, source);
			ms.Position = 0;
			var exn = Assert.Throws<ProtoException>(() => Serializer.Deserialize<HazEnforcedRequired>(ms));
			Assert.Equal("missing fields [15]", exn.Message);
		}
	}
}
