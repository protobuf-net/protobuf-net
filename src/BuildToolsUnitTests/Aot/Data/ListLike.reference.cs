using System.Reflection;
using AotFixtures.ListLike;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ListLikeModel : ISerializer<Holder>, ISerializer<NotAList>
{
	Holder ISerializer<Holder>.Read(ref ProtoReader.State state, Holder value)
	{
		if (value == null)
		{
			Holder holder = new Holder();
			value = holder;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				NotAList notAList = value.NotAList;
				notAList = state.ReadMessage(SerializerFeatures.CategoryRepeated, notAList, this);
				if (notAList != null)
				{
					value.NotAList = notAList;
				}
				break;
			}
			case 2:
			{
				int other = state.ReadInt32();
				value.Other = other;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Holder>.Write(ref ProtoWriter.State state, Holder value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		NotAList notAList = value.NotAList;
		state.WriteMessage(1, SerializerFeatures.CategoryRepeated, notAList, this);
		int other = value.Other;
		if (other != 0)
		{
			state.WriteInt32Varint(2, other);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Holder>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<NotAList>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	NotAList ISerializer<NotAList>.Read(ref ProtoReader.State state, NotAList value)
	{
		if (value == null)
		{
			NotAList notAList = new NotAList();
			value = notAList;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Label = text;
				}
				break;
			}
			case 2:
			{
				int count = state.ReadInt32();
				value.Count2 = count;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<NotAList>.Write(ref ProtoWriter.State state, NotAList value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		string label = value.Label;
		state.WriteString(1, label);
		int count = value.Count2;
		if (count != 0)
		{
			state.WriteInt32Varint(2, count);
		}
	}
}
public sealed class ListLikeModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ListLikeModel, T>();
	}
}
