using System.Reflection;
using AotFixtures.DefaultConverter;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___DefaultConverterModel : ISerializer<Converted>
{
	Converted ISerializer<Converted>.Read(ref ProtoReader.State state, Converted value)
	{
		if (value == null)
		{
			Converted converted = new Converted();
			value = converted;
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
			case 5:
			{
				double ratio = state.ReadDouble();
				value.Ratio = ratio;
				break;
			}
			case 6:
			{
				long big = state.ReadInt64();
				value.Big = big;
				break;
			}
			case 7:
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

	void ISerializer<Converted>.Write(ref ProtoWriter.State state, Converted value)
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
		if (ratio != 2.25)
		{
			state.WriteFieldHeader(5, WireType.Fixed64);
			state.WriteDouble(ratio);
		}
		long big = value.Big;
		if (big != -7L)
		{
			state.WriteFieldHeader(6, WireType.Variant);
			state.WriteInt64(big);
		}
		number = value.Plain;
		if (number != 9)
		{
			state.WriteInt32Varint(7, number);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Converted>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class DefaultConverterModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___DefaultConverterModel, T>();
	}
}
