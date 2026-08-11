using System.Reflection;
using AotFixtures.Defaults;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___DefaultsModel : ISerializer<Declared>, ISerializer<Parsed>
{
	Declared ISerializer<Declared>.Read(ref ProtoReader.State state, Declared value)
	{
		if (value == null)
		{
			Declared declared = new Declared();
			value = declared;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int plain = state.ReadInt32();
				value.Number = plain;
				break;
			}
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Text = text;
				}
				break;
			}
			case 3:
			{
				bool flag = state.ReadBoolean();
				value.Flag = flag;
				break;
			}
			case 4:
			{
				double ratio = state.ReadDouble();
				value.Ratio = ratio;
				break;
			}
			case 5:
			{
				long big = state.ReadInt64();
				value.Big = big;
				break;
			}
			case 6:
			{
				int plain = state.ReadInt32();
				value.Plain = plain;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Declared>.Write(ref ProtoWriter.State state, Declared value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int number = value.Number;
		if (number != 5)
		{
			state.WriteInt32Varint(1, number);
		}
		string text = value.Text;
		if (text != null)
		{
			string text2 = text;
			if (!(text2 == "abc"))
			{
				state.WriteString(2, text2);
			}
		}
		bool flag = value.Flag;
		if (!flag)
		{
			state.WriteFieldHeader(3, WireType.Variant);
			state.WriteBoolean(flag);
		}
		double ratio = value.Ratio;
		if (ratio != 2.5)
		{
			state.WriteFieldHeader(4, WireType.Fixed64);
			state.WriteDouble(ratio);
		}
		long big = value.Big;
		if (big != 7L)
		{
			state.WriteFieldHeader(5, WireType.Variant);
			state.WriteInt64(big);
		}
		number = value.Plain;
		if (number != 0)
		{
			state.WriteInt32Varint(6, number);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Declared>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Parsed>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Parsed ISerializer<Parsed>.Read(ref ProtoReader.State state, Parsed value)
	{
		if (value == null)
		{
			Parsed parsed = new Parsed();
			value = parsed;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				Shade byConverter = (Shade)state.ReadUInt16();
				value.ByName = byConverter;
				break;
			}
			case 2:
			{
				Shade byConverter = (Shade)state.ReadUInt16();
				value.ByValue = byConverter;
				break;
			}
			case 3:
			{
				char directChar = (char)state.ReadUInt16();
				value.Letter = directChar;
				break;
			}
			case 4:
			{
				char directChar = (char)state.ReadUInt16();
				value.DirectChar = directChar;
				break;
			}
			case 5:
			{
				Shade byConverter = (Shade)state.ReadUInt16();
				value.ByConverter = byConverter;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Parsed>.Write(ref ProtoWriter.State state, Parsed value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Shade byName = value.ByName;
		if (byName != Shade.Green)
		{
			state.WriteFieldHeader(1, WireType.Variant);
			ushort value2 = (ushort)byName;
			state.WriteUInt16(value2);
		}
		byName = value.ByValue;
		if (byName != Shade.Blue)
		{
			state.WriteFieldHeader(2, WireType.Variant);
			ushort value2 = (ushort)byName;
			state.WriteUInt16(value2);
		}
		char letter = value.Letter;
		if (letter != 'x')
		{
			state.WriteFieldHeader(3, WireType.Variant);
			ushort value2 = letter;
			state.WriteUInt16(value2);
		}
		letter = value.DirectChar;
		if (letter != 'y')
		{
			state.WriteFieldHeader(4, WireType.Variant);
			ushort value2 = letter;
			state.WriteUInt16(value2);
		}
		byName = value.ByConverter;
		if (byName != Shade.Red)
		{
			state.WriteFieldHeader(5, WireType.Variant);
			ushort value2 = (ushort)byName;
			state.WriteUInt16(value2);
		}
	}
}
public sealed class DefaultsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___DefaultsModel, T>();
	}
}
