using System.Reflection;
using AotFixtures.Derived;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___DerivedModel : ISerializer<Derives>, ISerializer<Ambiguous>
{
	Derives ISerializer<Derives>.Read(ref ProtoReader.State state, Derives value)
	{
		if (value == null)
		{
			Derives derives = new Derives();
			value = derives;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				MyList list = value.List;
				list = RepeatedSerializer.CreateList<MyList, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, list);
				if (list != null)
				{
					value.List = list;
				}
				break;
			}
			case 2:
			{
				MySet set = value.Set;
				set = RepeatedSerializer.CreateEnumerable<MySet, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, set);
				if (set != null)
				{
					value.Set = set;
				}
				break;
			}
			case 3:
			{
				MyQueue queue = value.Queue;
				queue = RepeatedSerializer.CreateQueue<MyQueue, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, queue);
				if (queue != null)
				{
					value.Queue = queue;
				}
				break;
			}
			case 4:
			{
				Ambiguous ambiguous = value.Ambiguous;
				ambiguous = state.ReadMessage(SerializerFeatures.CategoryRepeated, ambiguous, this);
				if (ambiguous != null)
				{
					value.Ambiguous = ambiguous;
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

	void ISerializer<Derives>.Write(ref ProtoWriter.State state, Derives value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		MyList list = value.List;
		if (list != null)
		{
			MyList values = list;
			RepeatedSerializer.CreateList<MyList, int>().WriteRepeated(ref state, 1, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		MySet set = value.Set;
		if (set != null)
		{
			MySet values2 = set;
			RepeatedSerializer.CreateEnumerable<MySet, int>().WriteRepeated(ref state, 2, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values2);
		}
		MyQueue queue = value.Queue;
		if (queue != null)
		{
			MyQueue values3 = queue;
			RepeatedSerializer.CreateQueue<MyQueue, int>().WriteRepeated(ref state, 3, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values3);
		}
		Ambiguous ambiguous = value.Ambiguous;
		state.WriteMessage(4, SerializerFeatures.CategoryRepeated, ambiguous, this);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Derives>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Ambiguous>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Ambiguous ISerializer<Ambiguous>.Read(ref ProtoReader.State state, Ambiguous value)
	{
		if (value == null)
		{
			Ambiguous ambiguous = new Ambiguous();
			value = ambiguous;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Label = text;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Ambiguous>.Write(ref ProtoWriter.State state, Ambiguous value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		string label = value.Label;
		state.WriteString(1, label);
	}
}
public sealed class DerivedModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___DerivedModel, T>();
	}
}
