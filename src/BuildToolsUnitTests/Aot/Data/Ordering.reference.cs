using System.Reflection;
using AotFixtures.Ordering;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___OrderingModel : ISerializer<ViaDataMember>, ISerializer<ViaDataMemberOffset>, ISerializer<ViaXmlElement>, ISerializer<OffsetIgnoredByXml>, ISerializer<Mixed>
{
	ViaDataMember ISerializer<ViaDataMember>.Read(ref ProtoReader.State state, ViaDataMember value)
	{
		if (value == null)
		{
			ViaDataMember viaDataMember = new ViaDataMember();
			value = viaDataMember;
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

	void ISerializer<ViaDataMember>.Write(ref ProtoWriter.State state, ViaDataMember value)
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

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<ViaDataMember>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<ViaDataMemberOffset>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<ViaXmlElement>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<OffsetIgnoredByXml>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Mixed>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	ViaDataMemberOffset ISerializer<ViaDataMemberOffset>.Read(ref ProtoReader.State state, ViaDataMemberOffset value)
	{
		if (value == null)
		{
			ViaDataMemberOffset viaDataMemberOffset = new ViaDataMemberOffset();
			value = viaDataMemberOffset;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 11:
			{
				int first = state.ReadInt32();
				value.First = first;
				break;
			}
			case 12:
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

	void ISerializer<ViaDataMemberOffset>.Write(ref ProtoWriter.State state, ViaDataMemberOffset value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int first = value.First;
		if (first != 0)
		{
			state.WriteInt32Varint(11, first);
		}
		string second = value.Second;
		state.WriteString(12, second);
	}

	ViaXmlElement ISerializer<ViaXmlElement>.Read(ref ProtoReader.State state, ViaXmlElement value)
	{
		if (value == null)
		{
			ViaXmlElement viaXmlElement = new ViaXmlElement();
			value = viaXmlElement;
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

	void ISerializer<ViaXmlElement>.Write(ref ProtoWriter.State state, ViaXmlElement value)
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

	OffsetIgnoredByXml ISerializer<OffsetIgnoredByXml>.Read(ref ProtoReader.State state, OffsetIgnoredByXml value)
	{
		if (value == null)
		{
			OffsetIgnoredByXml offsetIgnoredByXml = new OffsetIgnoredByXml();
			value = offsetIgnoredByXml;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				int first = state.ReadInt32();
				value.First = first;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<OffsetIgnoredByXml>.Write(ref ProtoWriter.State state, OffsetIgnoredByXml value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int first = value.First;
		if (first != 0)
		{
			state.WriteInt32Varint(1, first);
		}
	}

	Mixed ISerializer<Mixed>.Read(ref ProtoReader.State state, Mixed value)
	{
		if (value == null)
		{
			Mixed mixed = new Mixed();
			value = mixed;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 2:
			{
				int both = state.ReadInt32();
				value.OnlyDataMember = both;
				break;
			}
			case 5:
			{
				int both = state.ReadInt32();
				value.Both = both;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Mixed>.Write(ref ProtoWriter.State state, Mixed value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int onlyDataMember = value.OnlyDataMember;
		if (onlyDataMember != 0)
		{
			state.WriteInt32Varint(2, onlyDataMember);
		}
		onlyDataMember = value.Both;
		if (onlyDataMember != 0)
		{
			state.WriteInt32Varint(5, onlyDataMember);
		}
	}
}
public sealed class OrderingModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___OrderingModel, T>();
	}
}
