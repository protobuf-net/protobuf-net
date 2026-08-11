using System.Reflection;
using AotFixtures.ContractOptions;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ContractOptionsModel : ISerializer<Grouped>, ISerializer<Lenient>, ISerializer<LenientBase>, ISubTypeSerializer<LenientBase>, ISerializer<LenientDerived>, ISubTypeSerializer<LenientDerived>, ISerializer<ProtoOnly>, ISerializer<BothFamilies>
{
	Grouped ISerializer<Grouped>.Read(ref ProtoReader.State state, Grouped value)
	{
		if (value == null)
		{
			Grouped grouped = new Grouped();
			value = grouped;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int id = state.ReadInt32();
				value.Id = id;
				break;
			}
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Name = text;
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

	void ISerializer<Grouped>.Write(ref ProtoWriter.State state, Grouped value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int id = value.Id;
		if (id != 0)
		{
			state.WriteInt32Varint(1, id);
		}
		string name = value.Name;
		state.WriteString(2, name);
	}

	private SerializerFeatures Features_83()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeStartGroup | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Grouped>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_83
		return this.Features_83();
	}

	Lenient ISerializer<Lenient>.Read(ref ProtoReader.State state, Lenient value)
	{
		if (value == null)
		{
			Lenient lenient = new Lenient();
			value = lenient;
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

	void ISerializer<Lenient>.Write(ref ProtoWriter.State state, Lenient value)
	{
		int id = value.Id;
		if (id != 0)
		{
			state.WriteInt32Varint(1, id);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Lenient>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<LenientBase>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<LenientDerived>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<ProtoOnly>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<BothFamilies>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	LenientBase ISerializer<LenientBase>.Read(ref ProtoReader.State state, LenientBase value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return ((ISubTypeSerializer<LenientBase>)this).ReadSubType(ref state, SubTypeState<LenientBase>.Create(state.Context, value));
	}

	void ISerializer<LenientBase>.Write(ref ProtoWriter.State state, LenientBase value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<LenientBase>)this).WriteSubType(ref state, value);
	}

	void ISubTypeSerializer<LenientBase>.WriteSubType(ref ProtoWriter.State state, LenientBase value)
	{
		if (TypeModel.IsSubType(value) && value is LenientDerived value2)
		{
			state.WriteSubType(10, value2, this);
		}
		int id = value.Id;
		if (id != 0)
		{
			state.WriteInt32Varint(1, id);
		}
	}

	LenientBase ISubTypeSerializer<LenientBase>.ReadSubType(ref ProtoReader.State state, SubTypeState<LenientBase> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				LenientBase value2 = value.Value;
				int id = state.ReadInt32();
				value2.Id = id;
				break;
			}
			case 10:
				value.ReadSubType<LenientDerived>(ref state, this);
				break;
			default:
				state.SkipField();
				break;
			}
		}
		return value.Value;
	}

	LenientDerived ISerializer<LenientDerived>.Read(ref ProtoReader.State state, LenientDerived value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (LenientDerived)((ISubTypeSerializer<LenientBase>)this).ReadSubType(ref state, SubTypeState<LenientBase>.Create(state.Context, value));
	}

	void ISerializer<LenientDerived>.Write(ref ProtoWriter.State state, LenientDerived value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<LenientBase>)this).WriteSubType(ref state, (LenientBase)value);
	}

	void ISubTypeSerializer<LenientDerived>.WriteSubType(ref ProtoWriter.State state, LenientDerived value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		string extra = value.Extra;
		state.WriteString(2, extra);
	}

	LenientDerived ISubTypeSerializer<LenientDerived>.ReadSubType(ref ProtoReader.State state, SubTypeState<LenientDerived> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 2)
			{
				LenientDerived value2 = value.Value;
				string text = state.ReadString();
				if (text != null)
				{
					value2.Extra = text;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	ProtoOnly ISerializer<ProtoOnly>.Read(ref ProtoReader.State state, ProtoOnly value)
	{
		if (value == null)
		{
			ProtoOnly protoOnly = new ProtoOnly();
			value = protoOnly;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 3)
			{
				int tagged = state.ReadInt32();
				value.Tagged = tagged;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<ProtoOnly>.Write(ref ProtoWriter.State state, ProtoOnly value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int tagged = value.Tagged;
		if (tagged != 0)
		{
			state.WriteInt32Varint(3, tagged);
		}
	}

	BothFamilies ISerializer<BothFamilies>.Read(ref ProtoReader.State state, BothFamilies value)
	{
		if (value == null)
		{
			BothFamilies bothFamilies = new BothFamilies();
			value = bothFamilies;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int tagged = state.ReadInt32();
				value.Ordered = tagged;
				break;
			}
			case 3:
			{
				int tagged = state.ReadInt32();
				value.Tagged = tagged;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<BothFamilies>.Write(ref ProtoWriter.State state, BothFamilies value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int ordered = value.Ordered;
		if (ordered != 0)
		{
			state.WriteInt32Varint(1, ordered);
		}
		ordered = value.Tagged;
		if (ordered != 0)
		{
			state.WriteInt32Varint(3, ordered);
		}
	}
}
public sealed class ContractOptionsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ContractOptionsModel, T>();
	}
}
