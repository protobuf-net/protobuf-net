using System.Collections.Generic;
using System.Reflection;
using AotFixtures.MapNested;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___MapNestedModel : ISerializer<Nested>
{
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
			switch (num)
			{
			case 1:
			{
				Dictionary<int, List<int>> lists = value.Lists;
				lists = MapSerializer.CreateDictionary<int, List<int>>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, lists, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeVarint, null, this as ISerializer<List<int>>);
				if (lists != null)
				{
					value.Lists = lists;
				}
				break;
			}
			case 2:
			{
				Dictionary<long, long[]> arrays = value.Arrays;
				arrays = MapSerializer.CreateDictionary<long, long[]>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, arrays, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeVarint, null, this as ISerializer<long[]>);
				if (arrays != null)
				{
					value.Arrays = arrays;
				}
				break;
			}
			case 4:
			{
				Dictionary<float, List<int>> floatKeyed = value.FloatKeyed;
				floatKeyed = MapSerializer.CreateDictionary<float, List<int>>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, floatKeyed, SerializerFeatures.WireTypeFixed32, SerializerFeatures.WireTypeVarint, null, this as ISerializer<List<int>>);
				if (floatKeyed != null)
				{
					value.FloatKeyed = floatKeyed;
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

	void ISerializer<Nested>.Write(ref ProtoWriter.State state, Nested value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Dictionary<int, List<int>> lists = value.Lists;
		if (lists != null)
		{
			Dictionary<int, List<int>> values = lists;
			MapSerializer.CreateDictionary<int, List<int>>().WriteMap(ref state, 1, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, values, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeVarint, null, this as ISerializer<List<int>>);
		}
		Dictionary<long, long[]> arrays = value.Arrays;
		if (arrays != null)
		{
			Dictionary<long, long[]> values2 = arrays;
			MapSerializer.CreateDictionary<long, long[]>().WriteMap(ref state, 2, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, values2, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeVarint, null, this as ISerializer<long[]>);
		}
		Dictionary<float, List<int>> floatKeyed = value.FloatKeyed;
		if (floatKeyed != null)
		{
			Dictionary<float, List<int>> values3 = floatKeyed;
			MapSerializer.CreateDictionary<float, List<int>>().WriteMap(ref state, 4, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, values3, SerializerFeatures.WireTypeFixed32, SerializerFeatures.WireTypeVarint, null, this as ISerializer<List<int>>);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Nested>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class MapNestedModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___MapNestedModel, T>();
	}
}
