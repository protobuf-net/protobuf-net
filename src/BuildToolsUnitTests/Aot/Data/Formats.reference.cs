using System.Reflection;
using AotFixtures.Formats;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___FormatsModel : ISerializer<Formatted>, ISerializer<Inner>
{
	Formatted ISerializer<Formatted>.Read(ref ProtoReader.State state, Formatted value)
	{
		if (value == null)
		{
			Formatted formatted = new Formatted();
			value = formatted;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				state.Hint(WireType.SignedVariant);
				int twosComplement = state.ReadInt32();
				value.ZigZagInt = twosComplement;
				break;
			}
			case 2:
			{
				int twosComplement = state.ReadInt32();
				value.FixedInt = twosComplement;
				break;
			}
			case 3:
			{
				state.Hint(WireType.SignedVariant);
				long fixedLong = state.ReadInt64();
				value.ZigZagLong = fixedLong;
				break;
			}
			case 4:
			{
				long fixedLong = state.ReadInt64();
				value.FixedLong = fixedLong;
				break;
			}
			case 5:
			{
				int twosComplement = state.ReadInt32();
				value.TwosComplement = twosComplement;
				break;
			}
			case 6:
			{
				int twosComplement = state.ReadInt32();
				value.RequiredInt = twosComplement;
				break;
			}
			case 7:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.RequiredString = text;
				}
				break;
			}
			case 8:
			{
				Inner plain = value.Grouped;
				plain = state.ReadMessage(SerializerFeatures.CategoryRepeated, plain, this);
				if (plain != null)
				{
					value.Grouped = plain;
				}
				break;
			}
			case 9:
			{
				Inner plain = value.Plain;
				plain = state.ReadMessage(SerializerFeatures.CategoryRepeated, plain, this);
				if (plain != null)
				{
					value.Plain = plain;
				}
				break;
			}
			case 10:
			{
				int[] zigZagArray = value.ZigZagArray;
				zigZagArray = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeSignedVarint | SerializerFeatures.OptionPackedDisabled, zigZagArray);
				if (zigZagArray != null)
				{
					value.ZigZagArray = zigZagArray;
				}
				break;
			}
			case 11:
			{
				long[] packedFixed = value.PackedFixed;
				packedFixed = RepeatedSerializer.CreateVector<long>().ReadRepeated(ref state, SerializerFeatures.WireTypeFixed64, packedFixed);
				if (packedFixed != null)
				{
					value.PackedFixed = packedFixed;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Formatted>.Write(ref ProtoWriter.State state, Formatted value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int zigZagInt = value.ZigZagInt;
		if (zigZagInt != 0)
		{
			state.WriteFieldHeader(1, WireType.SignedVariant);
			state.WriteInt32(zigZagInt);
		}
		zigZagInt = value.FixedInt;
		if (zigZagInt != 0)
		{
			state.WriteFieldHeader(2, WireType.Fixed32);
			state.WriteInt32(zigZagInt);
		}
		long zigZagLong = value.ZigZagLong;
		if (zigZagLong != 0L)
		{
			state.WriteFieldHeader(3, WireType.SignedVariant);
			state.WriteInt64(zigZagLong);
		}
		zigZagLong = value.FixedLong;
		if (zigZagLong != 0L)
		{
			state.WriteFieldHeader(4, WireType.Fixed64);
			state.WriteInt64(zigZagLong);
		}
		zigZagInt = value.TwosComplement;
		if (zigZagInt != 0)
		{
			state.WriteInt32Varint(5, zigZagInt);
		}
		zigZagInt = value.RequiredInt;
		state.WriteInt32Varint(6, zigZagInt);
		string requiredString = value.RequiredString;
		state.WriteString(7, requiredString);
		Inner grouped = value.Grouped;
		state.WriteGroup(8, SerializerFeatures.CategoryRepeated, grouped, this);
		grouped = value.Plain;
		state.WriteMessage(9, SerializerFeatures.CategoryRepeated, grouped, this);
		int[] zigZagArray = value.ZigZagArray;
		if (zigZagArray != null)
		{
			int[] values = zigZagArray;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 10, SerializerFeatures.WireTypeSignedVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		long[] packedFixed = value.PackedFixed;
		if (packedFixed != null)
		{
			long[] values2 = packedFixed;
			RepeatedSerializer.CreateVector<long>().WriteRepeated(ref state, 11, SerializerFeatures.WireTypeFixed64, values2);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Formatted>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Inner>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Inner ISerializer<Inner>.Read(ref ProtoReader.State state, Inner value)
	{
		if (value == null)
		{
			Inner inner = new Inner();
			value = inner;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				int value2 = state.ReadInt32();
				value.Value = value2;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Inner>.Write(ref ProtoWriter.State state, Inner value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int value2 = value.Value;
		if (value2 != 0)
		{
			state.WriteInt32Varint(1, value2);
		}
	}
}
public sealed class FormatsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___FormatsModel, T>();
	}
}
