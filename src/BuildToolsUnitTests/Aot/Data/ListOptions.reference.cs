using System.Collections.Generic;
using System.Reflection;
using AotFixtures.ListOptions;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ListOptionsModel : ISerializer<Options>
{
	Options ISerializer<Options>.Read(ref ProtoReader.State state, Options value)
	{
		if (value == null)
		{
			Options options = new Options();
			value = options;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int[] notPacked = value.Default;
				notPacked = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, notPacked);
				if (notPacked != null)
				{
					value.Default = notPacked;
				}
				break;
			}
			case 2:
			{
				int[] notPacked = value.Packed;
				notPacked = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint, notPacked);
				if (notPacked != null)
				{
					value.Packed = notPacked;
				}
				break;
			}
			case 3:
			{
				List<int> overwrite = value.Overwrite;
				overwrite = RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionClearCollection, overwrite);
				if (overwrite != null)
				{
					value.Overwrite = overwrite;
				}
				break;
			}
			case 4:
			{
				List<int> overwrite = value.PackedOverwrite;
				overwrite = RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionClearCollection, overwrite);
				if (overwrite != null)
				{
					value.PackedOverwrite = overwrite;
				}
				break;
			}
			case 5:
			{
				int[] notPacked = value.NotPacked;
				notPacked = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, notPacked);
				if (notPacked != null)
				{
					value.NotPacked = notPacked;
				}
				break;
			}
			case 6:
			{
				double[] packedDouble = value.PackedDouble;
				packedDouble = RepeatedSerializer.CreateVector<double>().ReadRepeated(ref state, SerializerFeatures.WireTypeFixed64, packedDouble);
				if (packedDouble != null)
				{
					value.PackedDouble = packedDouble;
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

	void ISerializer<Options>.Write(ref ProtoWriter.State state, Options value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int[] array = value.Default;
		if (array != null)
		{
			int[] values = array;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 1, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		int[] packed = value.Packed;
		if (packed != null)
		{
			int[] values = packed;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 2, SerializerFeatures.WireTypeVarint, values);
		}
		List<int> overwrite = value.Overwrite;
		if (overwrite != null)
		{
			List<int> values2 = overwrite;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 3, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionClearCollection, values2);
		}
		List<int> packedOverwrite = value.PackedOverwrite;
		if (packedOverwrite != null)
		{
			List<int> values2 = packedOverwrite;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 4, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionClearCollection, values2);
		}
		int[] notPacked = value.NotPacked;
		if (notPacked != null)
		{
			int[] values = notPacked;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		double[] packedDouble = value.PackedDouble;
		if (packedDouble != null)
		{
			double[] values3 = packedDouble;
			RepeatedSerializer.CreateVector<double>().WriteRepeated(ref state, 6, SerializerFeatures.WireTypeFixed64, values3);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Options>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class ListOptionsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ListOptionsModel, T>();
	}
}
