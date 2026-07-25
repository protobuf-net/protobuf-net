using System;
using System.Collections.Generic;
using System.Reflection;
using AotFixtures.Getter;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___GetterModel : ISerializer<Getters>, ISerializer<Point>, ISerializer<Nested>
{
	Getters ISerializer<Getters>.Read(ref ProtoReader.State state, Getters value)
	{
		if (value == null)
		{
			Getters getters = new Getters();
			value = getters;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				List<int> numbers = value.Numbers;
				RepeatedSerializer.CreateList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, numbers);
				break;
			}
			case 2:
			{
				Dictionary<int, string> map = value.Map;
				MapSerializer.CreateDictionary<int, string>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, map, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString);
				break;
			}
			case 3:
			{
				Nested child = value.Child;
				state.ReadMessage(SerializerFeatures.CategoryRepeated, child, this);
				break;
			}
			case 4:
				state.ReadInt32();
				break;
			case 5:
				state.ReadString();
				break;
			case 6:
			{
				byte[] blob = value.Blob;
				state.AppendBytes(blob);
				break;
			}
			case 7:
				new int?(state.ReadInt32());
				break;
			case 8:
				state.ReadInt32();
				break;
			case 9:
				BclHelpers.ReadDateTime(ref state);
				break;
			case 10:
			{
				int[] array = value.Array;
				RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, array);
				break;
			}
			case 11:
			{
				Point valueOrDefault = value.Where;
				state.ReadMessage(SerializerFeatures.CategoryRepeated, valueOrDefault, this);
				break;
			}
			case 12:
			{
				Point valueOrDefault = value.Maybe2.GetValueOrDefault();
				new Point?(state.ReadMessage(SerializerFeatures.CategoryRepeated, valueOrDefault, this));
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Getters>.Write(ref ProtoWriter.State state, Getters value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		List<int> numbers = value.Numbers;
		if (numbers != null)
		{
			List<int> values = numbers;
			RepeatedSerializer.CreateList<int>().WriteRepeated(ref state, 1, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		Dictionary<int, string> map = value.Map;
		if (map != null)
		{
			Dictionary<int, string> values2 = map;
			MapSerializer.CreateDictionary<int, string>().WriteMap(ref state, 2, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values2, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString);
		}
		Nested child = value.Child;
		state.WriteMessage(3, SerializerFeatures.CategoryRepeated, child, this);
		int value2 = value.Value;
		if (value2 != 0)
		{
			state.WriteInt32Varint(4, value2);
		}
		string text = value.Text;
		state.WriteString(5, text);
		byte[] blob = value.Blob;
		if (blob != null)
		{
			state.WriteFieldHeader(6, WireType.String);
			byte[] data = blob;
			state.WriteBytes(data);
		}
		int? maybe = value.Maybe;
		if (maybe.HasValue)
		{
			value2 = maybe.GetValueOrDefault();
			state.WriteInt32Varint(7, value2);
		}
		Shade colour = value.Colour;
		if (colour != Shade.None)
		{
			value2 = (int)colour;
			state.WriteInt32Varint(8, value2);
		}
		DateTime when = value.When;
		state.WriteFieldHeader(9, WireType.String);
		DateTime value3 = when;
		BclHelpers.WriteDateTime(ref state, value3);
		int[] array = value.Array;
		if (array != null)
		{
			int[] values3 = array;
			RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 10, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values3);
		}
		Point value4 = value.Where;
		state.WriteMessage(11, SerializerFeatures.CategoryRepeated, value4, this);
		Point? maybe2 = value.Maybe2;
		if (maybe2.HasValue)
		{
			value4 = maybe2.GetValueOrDefault();
			state.WriteMessage(12, SerializerFeatures.CategoryRepeated, value4, this);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Getters>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Point>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Nested>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Point ISerializer<Point>.Read(ref ProtoReader.State state, Point value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				int x = state.ReadInt32();
				value.X = x;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Point>.Write(ref ProtoWriter.State state, Point value)
	{
		int x = value.X;
		if (x != 0)
		{
			state.WriteInt32Varint(1, x);
		}
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
public sealed class GetterModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___GetterModel, T>();
	}
}
