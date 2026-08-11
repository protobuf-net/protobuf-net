using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using AotFixtures.DynamicCategory;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___DynamicCategoryModel : ISerializer<Reading>, ISerializerProxy<Measure>, ISerializerProxy<Label>
{
	Reading ISerializer<Reading>.Read(ref ProtoReader.State state, Reading value)
	{
		if (value == null)
		{
			Reading reading = new Reading();
			value = reading;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				Measure scalar = value.Scalar;
				scalar = SerializerCache.Get<MeasureSerializer, Measure>().Read(ref state, scalar);
				value.Scalar = scalar;
				break;
			}
			case 2:
			{
				Label message = value.Message;
				message = state.ReadMessage(SerializerFeatures.CategoryRepeated, message, SerializerCache.Get<LabelSerializer, Label>());
				if (message != null)
				{
					value.Message = message;
				}
				break;
			}
			case 3:
			{
				int other = state.ReadInt32();
				value.Other = other;
				break;
			}
			case 4:
			{
				List<Measure> scalars = value.Scalars;
				scalars = RepeatedSerializer.CreateList<Measure>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, scalars, this as ISerializer<Measure>);
				if (scalars != null)
				{
					value.Scalars = scalars;
				}
				break;
			}
			case 5:
			{
				List<Label> messages = value.Messages;
				messages = RepeatedSerializer.CreateList<Label>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, messages, this as ISerializer<Label>);
				if (messages != null)
				{
					value.Messages = messages;
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

	void ISerializer<Reading>.Write(ref ProtoWriter.State state, Reading value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Measure scalar = value.Scalar;
		state.WriteFieldHeader(1, WireType.Variant);
		Measure value2 = scalar;
		SerializerCache.Get<MeasureSerializer, Measure>().Write(ref state, value2);
		Label message = value.Message;
		state.WriteMessage(2, SerializerFeatures.CategoryRepeated, message, SerializerCache.Get<LabelSerializer, Label>());
		int other = value.Other;
		if (other != 0)
		{
			state.WriteInt32Varint(3, other);
		}
		List<Measure> scalars = value.Scalars;
		if (scalars != null)
		{
			List<Measure> values = scalars;
			RepeatedSerializer.CreateList<Measure>().WriteRepeated(ref state, 4, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values, this as ISerializer<Measure>);
		}
		List<Label> messages = value.Messages;
		if (messages != null)
		{
			List<Label> values2 = messages;
			RepeatedSerializer.CreateList<Label>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values2, this as ISerializer<Label>);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Reading>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	[SpecialName]
	ISerializer<Measure> ISerializerProxy<Measure>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<MeasureSerializer, Measure>();
	}

	[SpecialName]
	ISerializer<Label> ISerializerProxy<Label>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<LabelSerializer, Label>();
	}
}
public sealed class DynamicCategoryModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___DynamicCategoryModel, T>();
	}
}
