using System.Collections.Generic;
using System.Reflection;
using AotFixtures.Wrapped;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___WrappedModel : ISerializer<Wrapped>, ISerializer<Nested>
{
	Wrapped ISerializer<Wrapped>.Read(ref ProtoReader.State state, Wrapped value)
	{
		if (value == null)
		{
			Wrapped wrapped = new Wrapped();
			value = wrapped;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int? value2 = value.Value;
				value2 = state.ReadAny(SerializerFeatures.OptionWrappedValue, value2);
				value.Value = value2;
				break;
			}
			case 2:
			{
				int? value2 = value.Grouped;
				value2 = state.ReadAny(SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup, value2);
				value.Grouped = value2;
				break;
			}
			case 3:
			{
				string text = value.Text;
				text = state.ReadAny(SerializerFeatures.OptionWrappedValue, text);
				if (text != null)
				{
					value.Text = text;
				}
				break;
			}
			case 4:
			{
				List<int?> bare = value.Ids;
				RepeatedSerializer.CreateList<int?>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence, bare);
				break;
			}
			case 5:
			{
				List<Nested> both = value.Items;
				RepeatedSerializer.CreateList<Nested>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence, both, this);
				break;
			}
			case 6:
			{
				List<Nested> both = value.GroupedItems;
				RepeatedSerializer.CreateList<Nested>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup | SerializerFeatures.OptionWrappedValueFieldPresence, both, this);
				break;
			}
			case 7:
			{
				Dictionary<int, Nested> groupedKeyed = value.Keyed;
				MapSerializer.CreateDictionary<int, Nested>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValueFieldPresence, groupedKeyed, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString | SerializerFeatures.OptionWrappedValue, null, this);
				break;
			}
			case 8:
			{
				List<int> groupedNumbers = value.Numbers;
				groupedNumbers = RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedCollection, groupedNumbers);
				if (groupedNumbers != null)
				{
					value.Numbers = groupedNumbers;
				}
				break;
			}
			case 9:
			{
				List<int> groupedNumbers = value.GroupedNumbers;
				groupedNumbers = RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedCollection | SerializerFeatures.OptionWrappedCollectionGroup, groupedNumbers);
				if (groupedNumbers != null)
				{
					value.GroupedNumbers = groupedNumbers;
				}
				break;
			}
			case 10:
			{
				List<Nested> both = value.Both;
				both = RepeatedSerializer.CreateList<Nested>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence | SerializerFeatures.OptionWrappedCollection, both, this);
				if (both != null)
				{
					value.Both = both;
				}
				break;
			}
			case 11:
			{
				byte[] blob = value.Blob;
				blob = state.ReadAny(SerializerFeatures.OptionWrappedValue, blob);
				if (blob != null)
				{
					value.Blob = blob;
				}
				break;
			}
			case 12:
			{
				Shade? colour = value.Colour;
				colour = state.ReadAny(SerializerFeatures.OptionWrappedValue, colour, this as ISerializer<Shade?>);
				value.Colour = colour;
				break;
			}
			case 13:
			{
				Dictionary<int, Nested> groupedKeyed = value.GroupedKeyed;
				MapSerializer.CreateDictionary<int, Nested>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValueFieldPresence, groupedKeyed, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup, null, this);
				break;
			}
			case 14:
			{
				Dictionary<int, int?> scalars = value.Scalars;
				MapSerializer.CreateDictionary<int, int?>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValueFieldPresence, scalars, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionWrappedValue);
				break;
			}
			case 15:
			{
				List<int?> bare = value.Bare;
				RepeatedSerializer.CreateList<int?>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, bare);
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Wrapped>.Write(ref ProtoWriter.State state, Wrapped value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int? value2 = value.Value;
		state.WriteAny(1, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionWrappedValue, value2);
		value2 = value.Grouped;
		state.WriteAny(2, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup, value2);
		string text = value.Text;
		state.WriteAny(3, SerializerFeatures.WireTypeString | SerializerFeatures.OptionWrappedValue, text);
		List<int?> ids = value.Ids;
		if (ids != null)
		{
			List<int?> values = ids;
			RepeatedSerializer.CreateList<int?>().WriteRepeated(ref state, 4, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence, values);
		}
		List<Nested> items = value.Items;
		if (items != null)
		{
			List<Nested> values2 = items;
			RepeatedSerializer.CreateList<Nested>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence, values2, this);
		}
		List<Nested> groupedItems = value.GroupedItems;
		if (groupedItems != null)
		{
			List<Nested> values2 = groupedItems;
			RepeatedSerializer.CreateList<Nested>().WriteRepeated(ref state, 6, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup | SerializerFeatures.OptionWrappedValueFieldPresence, values2, this);
		}
		Dictionary<int, Nested> keyed = value.Keyed;
		if (keyed != null)
		{
			Dictionary<int, Nested> values3 = keyed;
			MapSerializer.CreateDictionary<int, Nested>().WriteMap(ref state, 7, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValueFieldPresence, values3, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString | SerializerFeatures.OptionWrappedValue, null, this);
		}
		List<int> numbers = value.Numbers;
		if (numbers != null)
		{
			List<int> values4 = numbers;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 8, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedCollection, values4);
		}
		List<int> groupedNumbers = value.GroupedNumbers;
		if (groupedNumbers != null)
		{
			List<int> values4 = groupedNumbers;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 9, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedCollection | SerializerFeatures.OptionWrappedCollectionGroup, values4);
		}
		List<Nested> both = value.Both;
		if (both != null)
		{
			List<Nested> values2 = both;
			RepeatedSerializer.CreateList<Nested>().WriteRepeated(ref state, 10, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence | SerializerFeatures.OptionWrappedCollection, values2, this);
		}
		byte[] blob = value.Blob;
		state.WriteAny(11, SerializerFeatures.WireTypeString | SerializerFeatures.OptionWrappedValue, blob);
		Shade? colour = value.Colour;
		state.WriteAny(12, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionWrappedValue, colour, this as ISerializer<Shade?>);
		Dictionary<int, Nested> groupedKeyed = value.GroupedKeyed;
		if (groupedKeyed != null)
		{
			Dictionary<int, Nested> values3 = groupedKeyed;
			MapSerializer.CreateDictionary<int, Nested>().WriteMap(ref state, 13, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValueFieldPresence, values3, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString | SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup, null, this);
		}
		Dictionary<int, int?> scalars = value.Scalars;
		if (scalars != null)
		{
			Dictionary<int, int?> values5 = scalars;
			MapSerializer.CreateDictionary<int, int?>().WriteMap(ref state, 14, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionWrappedValueFieldPresence, values5, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionWrappedValue);
		}
		List<int?> bare = value.Bare;
		if (bare != null)
		{
			List<int?> values = bare;
			RepeatedSerializer.CreateList<int?>().WriteRepeated(ref state, 15, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Wrapped>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Nested>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
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
public sealed class WrappedModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___WrappedModel, T>();
	}
}
