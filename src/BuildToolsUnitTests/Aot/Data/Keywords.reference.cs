using System.Collections.Generic;
using System.Reflection;
using AotFixtures.Keywords;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___KeywordsModel : ISerializer<Keywords>, ISerializer<Inner>, ISerializer<Pair>
{
	Keywords ISerializer<Keywords>.Read(ref ProtoReader.State state, Keywords value)
	{
		if (value == null)
		{
			Keywords keywords = new Keywords();
			value = keywords;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int value2 = state.ReadInt32();
				value.@case = value2;
				break;
			}
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.@event = text;
				}
				break;
			}
			case 3:
			{
				List<int> values = value.@params;
				values = RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
				if (values != null)
				{
					value.@params = values;
				}
				break;
			}
			case 4:
			{
				Inner value4 = value.@class;
				value4 = state.ReadMessage(SerializerFeatures.CategoryRepeated, value4, this);
				if (value4 != null)
				{
					value.@class = value4;
				}
				break;
			}
			case 5:
			{
				Pair value3 = value.@lock;
				value3 = state.ReadMessage(SerializerFeatures.CategoryRepeated, value3, this);
				if (value3 != null)
				{
					value.@lock = value3;
				}
				break;
			}
			case 6:
			{
				int value2 = state.ReadInt32();
				value.value = value2;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Keywords>.Write(ref ProtoWriter.State state, Keywords value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int num = value.@case;
		if (num != 0)
		{
			state.WriteInt32Varint(1, num);
		}
		string value2 = value.@event;
		state.WriteString(2, value2);
		List<int> list = value.@params;
		if (list != null)
		{
			List<int> values = list;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 3, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		Inner value3 = value.@class;
		state.WriteMessage(4, SerializerFeatures.CategoryRepeated, value3, this);
		Pair value4 = value.@lock;
		state.WriteMessage(5, SerializerFeatures.CategoryRepeated, value4, this);
		num = value.value;
		if (num != 0)
		{
			state.WriteInt32Varint(6, num);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Keywords>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Inner>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Pair>.get_Features()
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
				int num2 = state.ReadInt32();
				value.@int = num2;
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
		int num = value.@int;
		if (num != 0)
		{
			state.WriteInt32Varint(1, num);
		}
	}

	Pair ISerializer<Pair>.Read(ref ProtoReader.State state, Pair value)
	{
		int num = 0;
		string text = null;
		if (value != null)
		{
			num = value.@if;
			text = value.@else;
		}
		int num2;
		while ((num2 = state.ReadFieldHeader()) > 0)
		{
			switch (num2)
			{
			case 1:
				num = state.ReadInt32();
				break;
			case 2:
			{
				string text2 = state.ReadString();
				if (text2 != null)
				{
					text = text2;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		value = new Pair(num, text);
		return value;
	}

	void ISerializer<Pair>.Write(ref ProtoWriter.State state, Pair value)
	{
		int value2 = value.@if;
		state.WriteInt32Varint(1, value2);
		string value3 = value.@else;
		state.WriteString(2, value3);
	}
}
public sealed class KeywordsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___KeywordsModel, T>();
	}
}
