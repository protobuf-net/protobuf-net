using System.Reflection;
using AotFixtures.IncludeGroup;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___IncludeGroupModel : ISerializer<Base>, ISubTypeSerializer<Base>, ISerializer<Grouped>, ISubTypeSerializer<Grouped>, ISerializer<Plain>, ISubTypeSerializer<Plain>
{
	Base ISerializer<Base>.Read(ref ProtoReader.State state, Base value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return ((ISubTypeSerializer<Base>)this).ReadSubType(ref state, SubTypeState<Base>.Create(state.Context, value));
	}

	void ISerializer<Base>.Write(ref ProtoWriter.State state, Base value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<Base>)this).WriteSubType(ref state, value);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Base>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Grouped>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Plain>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	void ISubTypeSerializer<Base>.WriteSubType(ref ProtoWriter.State state, Base value)
	{
		if (TypeModel.IsSubType(value))
		{
			if (value is Grouped value2)
			{
				state.WriteFieldHeader(3, WireType.StartGroup);
				state.WriteSubType(value2, this);
			}
			else if (value is Plain value3)
			{
				state.WriteSubType(4, value3, this);
			}
			else
			{
				TypeModel.ThrowUnexpectedSubtype(value);
			}
		}
		bool success = value.Success;
		if (success)
		{
			state.WriteFieldHeader(1, WireType.Variant);
			state.WriteBoolean(success);
		}
		string error = value.Error;
		state.WriteString(2, error);
	}

	Base ISubTypeSerializer<Base>.ReadSubType(ref ProtoReader.State state, SubTypeState<Base> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				Base value2 = value.Value;
				bool success = state.ReadBoolean();
				value2.Success = success;
				break;
			}
			case 2:
			{
				Base value2 = value.Value;
				string text = state.ReadString();
				if (text != null)
				{
					value2.Error = text;
				}
				break;
			}
			case 3:
				value.ReadSubType<Grouped>(ref state, this);
				break;
			case 4:
				value.ReadSubType<Plain>(ref state, this);
				break;
			default:
				state.SkipField();
				break;
			}
		}
		return value.Value;
	}

	Grouped ISerializer<Grouped>.Read(ref ProtoReader.State state, Grouped value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (Grouped)((ISubTypeSerializer<Base>)this).ReadSubType(ref state, SubTypeState<Base>.Create(state.Context, value));
	}

	void ISerializer<Grouped>.Write(ref ProtoWriter.State state, Grouped value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<Base>)this).WriteSubType(ref state, (Base)value);
	}

	void ISubTypeSerializer<Grouped>.WriteSubType(ref ProtoWriter.State state, Grouped value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int extra = value.Extra;
		if (extra != 0)
		{
			state.WriteInt32Varint(1, extra);
		}
	}

	Grouped ISubTypeSerializer<Grouped>.ReadSubType(ref ProtoReader.State state, SubTypeState<Grouped> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				Grouped value2 = value.Value;
				int extra = state.ReadInt32();
				value2.Extra = extra;
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	Plain ISerializer<Plain>.Read(ref ProtoReader.State state, Plain value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (Plain)((ISubTypeSerializer<Base>)this).ReadSubType(ref state, SubTypeState<Base>.Create(state.Context, value));
	}

	void ISerializer<Plain>.Write(ref ProtoWriter.State state, Plain value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<Base>)this).WriteSubType(ref state, (Base)value);
	}

	void ISubTypeSerializer<Plain>.WriteSubType(ref ProtoWriter.State state, Plain value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int extra = value.Extra;
		if (extra != 0)
		{
			state.WriteInt32Varint(1, extra);
		}
	}

	Plain ISubTypeSerializer<Plain>.ReadSubType(ref ProtoReader.State state, SubTypeState<Plain> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				Plain value2 = value.Value;
				int extra = state.ReadInt32();
				value2.Extra = extra;
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}
}
public sealed class IncludeGroupModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___IncludeGroupModel, T>();
	}
}
