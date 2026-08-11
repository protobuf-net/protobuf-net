using System.Reflection;
using AotFixtures.SchemaOnly;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___SchemaOnlyModel : ISerializer<Empty>, ISerializer<Plain>, ISerializer<SchemaOnly>, ISerializer<Ignoring>, ISerializer<EmptyExtensible>
{
	Empty ISerializer<Empty>.Read(ref ProtoReader.State state, Empty value)
	{
		if (value == null)
		{
			Empty empty = new Empty();
			value = empty;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			state.SkipField();
		}
		return value;
	}

	void ISerializer<Empty>.Write(ref ProtoWriter.State state, Empty value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		TypeModel.ThrowUnexpectedSubtype(value);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Empty>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Plain>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<SchemaOnly>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Ignoring>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<EmptyExtensible>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Plain ISerializer<Plain>.Read(ref ProtoReader.State state, Plain value)
	{
		if (value == null)
		{
			Plain plain = new Plain();
			value = plain;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int value2 = state.ReadInt32();
				value.Value = value2;
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
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Plain>.Write(ref ProtoWriter.State state, Plain value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int value2 = value.Value;
		if (value2 != 0)
		{
			state.WriteInt32Varint(1, value2);
		}
		string text = value.Text;
		state.WriteString(2, text);
	}

	SchemaOnly ISerializer<SchemaOnly>.Read(ref ProtoReader.State state, SchemaOnly value)
	{
		if (value == null)
		{
			SchemaOnly schemaOnly = new SchemaOnly();
			value = schemaOnly;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int value2 = state.ReadInt32();
				value.Value = value2;
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
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<SchemaOnly>.Write(ref ProtoWriter.State state, SchemaOnly value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int value2 = value.Value;
		if (value2 != 0)
		{
			state.WriteInt32Varint(1, value2);
		}
		string text = value.Text;
		state.WriteString(2, text);
	}

	Ignoring ISerializer<Ignoring>.Read(ref ProtoReader.State state, Ignoring value)
	{
		if (value == null)
		{
			Ignoring ignoring = new Ignoring();
			value = ignoring;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int value2 = state.ReadInt32();
				value.Value = value2;
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
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Ignoring>.Write(ref ProtoWriter.State state, Ignoring value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int value2 = value.Value;
		if (value2 != 0)
		{
			state.WriteInt32Varint(1, value2);
		}
		string text = value.Text;
		state.WriteString(2, text);
	}

	EmptyExtensible ISerializer<EmptyExtensible>.Read(ref ProtoReader.State state, EmptyExtensible value)
	{
		if (value == null)
		{
			EmptyExtensible emptyExtensible = new EmptyExtensible();
			value = emptyExtensible;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			state.AppendExtensionData(value);
		}
		return value;
	}

	void ISerializer<EmptyExtensible>.Write(ref ProtoWriter.State state, EmptyExtensible value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		state.AppendExtensionData(value);
	}
}
public sealed class SchemaOnlyModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___SchemaOnlyModel, T>();
	}
}
