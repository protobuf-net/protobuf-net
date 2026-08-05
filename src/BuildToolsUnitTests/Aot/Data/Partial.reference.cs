using System.Reflection;
using AotFixtures.Partial;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___PartialModel : ISerializer<Described>, ISerializer<Contested>, ISerializer<Excluded>, ISerializer<Mixed>
{
	Described ISerializer<Described>.Read(ref ProtoReader.State state, Described value)
	{
		if (value == null)
		{
			Described described = new Described();
			value = described;
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
			case 3:
			{
				int id = state.ReadInt32();
				value.Fixed = id;
				break;
			}
			case 4:
			{
				int id = state.ReadInt32();
				value.Always = id;
				break;
			}
			case 5:
			{
				int[] values = value.Values;
				values = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint, values);
				if (values != null)
				{
					value.Values = values;
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

	void ISerializer<Described>.Write(ref ProtoWriter.State state, Described value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int id = value.Id;
		if (id != 0)
		{
			state.WriteInt32Varint(1, id);
		}
		string name = value.Name;
		state.WriteString(2, name);
		id = value.Fixed;
		if (id != 0)
		{
			state.WriteFieldHeader(3, WireType.Fixed32);
			state.WriteInt32(id);
		}
		id = value.Always;
		state.WriteInt32Varint(4, id);
		int[] values = value.Values;
		if (values != null)
		{
			int[] values2 = values;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeVarint, values2);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Described>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Contested>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Excluded>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Mixed>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Contested ISerializer<Contested>.Read(ref ProtoReader.State state, Contested value)
	{
		if (value == null)
		{
			Contested contested = new Contested();
			value = contested;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int fromPartial = state.ReadInt32();
				value.Pinned = fromPartial;
				break;
			}
			case 2:
			{
				int fromPartial = state.ReadInt32();
				value.FromPartial = fromPartial;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Contested>.Write(ref ProtoWriter.State state, Contested value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int pinned = value.Pinned;
		if (pinned != 0)
		{
			state.WriteInt32Varint(1, pinned);
		}
		pinned = value.FromPartial;
		if (pinned != 0)
		{
			state.WriteInt32Varint(2, pinned);
		}
	}

	Excluded ISerializer<Excluded>.Read(ref ProtoReader.State state, Excluded value)
	{
		if (value == null)
		{
			Excluded excluded = new Excluded();
			value = excluded;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				int kept = state.ReadInt32();
				value.Kept = kept;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Excluded>.Write(ref ProtoWriter.State state, Excluded value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int kept = value.Kept;
		if (kept != 0)
		{
			state.WriteInt32Varint(1, kept);
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
				value.OrderOnly = both;
				break;
			}
			case 7:
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
		int orderOnly = value.OrderOnly;
		if (orderOnly != 0)
		{
			state.WriteInt32Varint(2, orderOnly);
		}
		orderOnly = value.Both;
		if (orderOnly != 0)
		{
			state.WriteInt32Varint(7, orderOnly);
		}
	}
}
public sealed class PartialModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___PartialModel, T>();
	}
}
