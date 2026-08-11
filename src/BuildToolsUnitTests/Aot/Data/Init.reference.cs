using System.Reflection;
using AotFixtures.Init;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___InitModel : ISerializer<Inits>, ISerializer<InitStruct>, ISerializer<Nested>
{
	Inits ISerializer<Inits>.Read(ref ProtoReader.State state, Inits value)
	{
		if (value == null)
		{
			Inits inits = new Inits();
			value = inits;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int mutable = state.ReadInt32();
				value.Number = mutable;
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
				int mutable = state.ReadInt32();
				value.Mutable = mutable;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Inits>.Write(ref ProtoWriter.State state, Inits value)
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
		number = value.Mutable;
		if (number != 0)
		{
			state.WriteInt32Varint(4, number);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Inits>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<InitStruct>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Nested>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	InitStruct ISerializer<InitStruct>.Read(ref ProtoReader.State state, InitStruct value)
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

	void ISerializer<InitStruct>.Write(ref ProtoWriter.State state, InitStruct value)
	{
		int number = value.Number;
		if (number != 0)
		{
			state.WriteInt32Varint(1, number);
		}
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
public sealed class InitModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___InitModel, T>();
	}
}
