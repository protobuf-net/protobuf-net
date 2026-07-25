using System.Collections.Generic;
using System.Reflection;
using AotFixtures.Lists;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ListsModel : ISerializer<Repeated>, ISerializer<Inner>
{
	Repeated ISerializer<Repeated>.Read(ref ProtoReader.State state, Repeated value)
	{
		if (value == null)
		{
			Repeated repeated = new Repeated();
			value = repeated;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int[] int32Array = value.Int32Array;
				int32Array = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, int32Array);
				if (int32Array != null)
				{
					value.Int32Array = int32Array;
				}
				break;
			}
			case 2:
			{
				List<int> int32List = value.Int32List;
				int32List = RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, int32List);
				if (int32List != null)
				{
					value.Int32List = int32List;
				}
				break;
			}
			case 3:
			{
				double[] doubleArray = value.DoubleArray;
				doubleArray = RepeatedSerializer.CreateVector<double>().ReadRepeated(ref state, SerializerFeatures.WireTypeFixed64 | SerializerFeatures.OptionPackedDisabled, doubleArray);
				if (doubleArray != null)
				{
					value.DoubleArray = doubleArray;
				}
				break;
			}
			case 4:
			{
				float[] singleArray = value.SingleArray;
				singleArray = RepeatedSerializer.CreateVector<float>().ReadRepeated(ref state, SerializerFeatures.WireTypeFixed32 | SerializerFeatures.OptionPackedDisabled, singleArray);
				if (singleArray != null)
				{
					value.SingleArray = singleArray;
				}
				break;
			}
			case 5:
			{
				List<bool> boolList = value.BoolList;
				boolList = RepeatedSerializer.CreateList<bool>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, boolList);
				if (boolList != null)
				{
					value.BoolList = boolList;
				}
				break;
			}
			case 6:
			{
				string[] stringArray = value.StringArray;
				stringArray = RepeatedSerializer.CreateVector<string>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, stringArray);
				if (stringArray != null)
				{
					value.StringArray = stringArray;
				}
				break;
			}
			case 7:
			{
				List<string> stringList = value.StringList;
				stringList = RepeatedSerializer.CreateList<string>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, stringList);
				if (stringList != null)
				{
					value.StringList = stringList;
				}
				break;
			}
			case 9:
			{
				List<Inner> messages = value.Messages;
				messages = RepeatedSerializer.CreateList<Inner>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, messages, this);
				if (messages != null)
				{
					value.Messages = messages;
				}
				break;
			}
			case 10:
			{
				Inner[] messageArray = value.MessageArray;
				messageArray = RepeatedSerializer.CreateVector<Inner>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, messageArray, this);
				if (messageArray != null)
				{
					value.MessageArray = messageArray;
				}
				break;
			}
			case 11:
			{
				int scalar = state.ReadInt32();
				value.Scalar = scalar;
				break;
			}
			case 12:
			{
				List<Colour> colours = value.Colours;
				colours = RepeatedSerializer.CreateList<Colour>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, colours, this as ISerializer<Colour>);
				if (colours != null)
				{
					value.Colours = colours;
				}
				break;
			}
			case 13:
			{
				Small[] smalls = value.Smalls;
				smalls = RepeatedSerializer.CreateVector<Small>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, smalls, this as ISerializer<Small>);
				if (smalls != null)
				{
					value.Smalls = smalls;
				}
				break;
			}
			case 14:
			{
				Colour singleColour = (Colour)state.ReadInt32();
				value.SingleColour = singleColour;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Repeated>.Write(ref ProtoWriter.State state, Repeated value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int[] int32Array = value.Int32Array;
		if (int32Array != null)
		{
			int[] values = int32Array;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 1, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		List<int> int32List = value.Int32List;
		if (int32List != null)
		{
			List<int> values2 = int32List;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 2, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values2);
		}
		double[] doubleArray = value.DoubleArray;
		if (doubleArray != null)
		{
			double[] values3 = doubleArray;
			RepeatedSerializer.CreateVector<double>().WriteRepeated(ref state, 3, SerializerFeatures.WireTypeFixed64 | SerializerFeatures.OptionPackedDisabled, values3);
		}
		float[] singleArray = value.SingleArray;
		if (singleArray != null)
		{
			float[] values4 = singleArray;
			RepeatedSerializer.CreateVector<float>().WriteRepeated(ref state, 4, SerializerFeatures.WireTypeFixed32 | SerializerFeatures.OptionPackedDisabled, values4);
		}
		List<bool> boolList = value.BoolList;
		if (boolList != null)
		{
			List<bool> values5 = boolList;
			RepeatedSerializer.CreateList<bool>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values5);
		}
		string[] stringArray = value.StringArray;
		if (stringArray != null)
		{
			string[] values6 = stringArray;
			RepeatedSerializer.CreateVector<string>().WriteRepeated(ref state, 6, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values6);
		}
		List<string> stringList = value.StringList;
		if (stringList != null)
		{
			List<string> values7 = stringList;
			RepeatedSerializer.CreateList<string>().WriteRepeated(ref state, 7, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values7);
		}
		List<Inner> messages = value.Messages;
		if (messages != null)
		{
			List<Inner> values8 = messages;
			RepeatedSerializer.CreateList<Inner>().WriteRepeated(ref state, 9, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values8, this);
		}
		Inner[] messageArray = value.MessageArray;
		if (messageArray != null)
		{
			Inner[] values9 = messageArray;
			RepeatedSerializer.CreateVector<Inner>().WriteRepeated(ref state, 10, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values9, this);
		}
		int scalar = value.Scalar;
		if (scalar != 0)
		{
			state.WriteInt32Varint(11, scalar);
		}
		List<Colour> colours = value.Colours;
		if (colours != null)
		{
			List<Colour> values10 = colours;
			RepeatedSerializer.CreateList<Colour>().WriteRepeated(ref state, 12, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values10, this as ISerializer<Colour>);
		}
		Small[] smalls = value.Smalls;
		if (smalls != null)
		{
			Small[] values11 = smalls;
			RepeatedSerializer.CreateVector<Small>().WriteRepeated(ref state, 13, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values11, this as ISerializer<Small>);
		}
		Colour singleColour = value.SingleColour;
		if (singleColour != Colour.None)
		{
			scalar = (int)singleColour;
			state.WriteInt32Varint(14, scalar);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Repeated>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Inner>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Inner ISerializer<Inner>.Read(ref ProtoReader.State state, Inner value)
	{
		if (value == null)
		{
			Inner inner = new Inner();
			value = inner;
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
					value.Label = text;
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

	void ISerializer<Inner>.Write(ref ProtoWriter.State state, Inner value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int value2 = value.Value;
		if (value2 != 0)
		{
			state.WriteInt32Varint(1, value2);
		}
		string label = value.Label;
		state.WriteString(2, label);
	}
}
public sealed class ListsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ListsModel, T>();
	}
}
