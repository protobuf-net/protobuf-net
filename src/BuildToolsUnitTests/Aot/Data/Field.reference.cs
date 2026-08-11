using System.Reflection;
using AotFixtures.Field;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___FieldModel : ISerializer<Fields>, ISerializer<FieldStruct>, ISerializer<DataFields>, ISerializer<Nested>
{
	Fields ISerializer<Fields>.Read(ref ProtoReader.State state, Fields value)
	{
		if (value == null)
		{
			Fields fields = new Fields();
			value = fields;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int property = state.ReadInt32();
				value.Number = property;
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
				Nested message = value.Message;
				message = state.ReadMessage(SerializerFeatures.CategoryRepeated, message, this);
				if (message != null)
				{
					value.Message = message;
				}
				break;
			}
			case 4:
			{
				state.Hint(WireType.SignedVariant);
				int property = state.ReadInt32();
				value.Zig = property;
				break;
			}
			case 5:
			{
				int property = state.ReadInt32();
				value.Defaulted = property;
				break;
			}
			case 6:
			{
				int? nullable = state.ReadInt32();
				value.Nullable = nullable;
				break;
			}
			case 7:
			{
				int property = state.ReadInt32();
				value.Property = property;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Fields>.Write(ref ProtoWriter.State state, Fields value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int number = value.Number;
		if (number != 0)
		{
			state.WriteInt32Varint(1, number);
		}
		string text = value.Text;
		state.WriteString(2, text);
		Nested message = value.Message;
		state.WriteMessage(3, SerializerFeatures.CategoryRepeated, message, this);
		number = value.Zig;
		if (number != 0)
		{
			state.WriteFieldHeader(4, WireType.SignedVariant);
			state.WriteInt32(number);
		}
		number = value.Defaulted;
		if (number != 7)
		{
			state.WriteInt32Varint(5, number);
		}
		int? nullable = value.Nullable;
		if (nullable.HasValue)
		{
			number = nullable.GetValueOrDefault();
			state.WriteInt32Varint(6, number);
		}
		number = value.Property;
		if (number != 0)
		{
			state.WriteInt32Varint(7, number);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Fields>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<FieldStruct>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<DataFields>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Nested>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	FieldStruct ISerializer<FieldStruct>.Read(ref ProtoReader.State state, FieldStruct value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				int number = state.ReadInt32();
				value.Number = number;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<FieldStruct>.Write(ref ProtoWriter.State state, FieldStruct value)
	{
		int number = value.Number;
		if (number != 0)
		{
			state.WriteInt32Varint(1, number);
		}
	}

	DataFields ISerializer<DataFields>.Read(ref ProtoReader.State state, DataFields value)
	{
		if (value == null)
		{
			DataFields dataFields = new DataFields();
			value = dataFields;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int first = state.ReadInt32();
				value.First = first;
				break;
			}
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Second = text;
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

	void ISerializer<DataFields>.Write(ref ProtoWriter.State state, DataFields value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int first = value.First;
		if (first != 0)
		{
			state.WriteInt32Varint(1, first);
		}
		string second = value.Second;
		state.WriteString(2, second);
	}

	Nested ISerializer<Nested>.Read(ref ProtoReader.State state, Nested value)
	{
		if (value == null)
		{
			Nested nested = new Nested();
			value = nested;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				int id = state.ReadInt32();
				value.Id = id;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Nested>.Write(ref ProtoWriter.State state, Nested value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int id = value.Id;
		if (id != 0)
		{
			state.WriteInt32Varint(1, id);
		}
	}
}
public sealed class FieldModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___FieldModel, T>();
	}
}
