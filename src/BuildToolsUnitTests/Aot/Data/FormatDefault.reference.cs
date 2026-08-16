using System;
using System.Collections.Generic;
using System.Reflection;
using AotFixtures.FormatDefault;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___FormatDefaultModel : ISerializer<Payment>, ISerializer<TimestampPromotion>
{
	Payment ISerializer<Payment>.Read(ref ProtoReader.State state, Payment value)
	{
		if (value == null)
		{
			Payment payment = new Payment();
			value = payment;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				Guid id = BclHelpers.ReadGuidBytes(ref state);
				value.Id = id;
				break;
			}
			case 2:
			{
				Guid? correlation = BclHelpers.ReadGuidBytes(ref state);
				value.Correlation = correlation;
				break;
			}
			case 3:
			{
				List<Guid> batch = value.Batch;
				batch = RepeatedSerializer.CreateList<Guid>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, batch, TypeModel.GetInbuiltSerializer<Guid>(CompatibilityLevel.Level300, DataFormat.FixedSize));
				if (batch != null)
				{
					value.Batch = batch;
				}
				break;
			}
			case 4:
			{
				state.Hint(WireType.SignedVariant);
				int stated = state.ReadInt32();
				value.Amount = stated;
				break;
			}
			case 5:
			{
				int stated = state.ReadInt32();
				value.Stated = stated;
				break;
			}
			case 6:
			{
				Dictionary<int, Guid> byId = value.ById;
				byId = MapSerializer.CreateDictionary<int, Guid>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, byId, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString, null, TypeModel.GetInbuiltSerializer<Guid>(CompatibilityLevel.Level300));
				if (byId != null)
				{
					value.ById = byId;
				}
				break;
			}
			case 7:
			{
				List<Guid?> certs = value.Certs;
				certs = RepeatedSerializer.CreateList<Guid?>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, certs, TypeModel.GetInbuiltSerializer<Guid?>(CompatibilityLevel.Level300, DataFormat.FixedSize));
				if (certs != null)
				{
					value.Certs = certs;
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

	void ISerializer<Payment>.Write(ref ProtoWriter.State state, Payment value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Guid id = value.Id;
		if (!(id == Guid.Empty))
		{
			state.WriteFieldHeader(1, WireType.String);
			BclHelpers.WriteGuidBytes(ref state, id);
		}
		Guid? correlation = value.Correlation;
		if (correlation.HasValue)
		{
			Guid valueOrDefault = correlation.GetValueOrDefault();
			state.WriteFieldHeader(2, WireType.String);
			id = valueOrDefault;
			BclHelpers.WriteGuidBytes(ref state, id);
		}
		List<Guid> batch = value.Batch;
		if (batch != null)
		{
			List<Guid> values = batch;
			RepeatedSerializer.CreateList<Guid>().WriteRepeated(ref state, 3, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values, TypeModel.GetInbuiltSerializer<Guid>(CompatibilityLevel.Level300, DataFormat.FixedSize));
		}
		int amount = value.Amount;
		if (amount != 0)
		{
			state.WriteFieldHeader(4, WireType.SignedVariant);
			state.WriteInt32(amount);
		}
		amount = value.Stated;
		if (amount != 0)
		{
			state.WriteFieldHeader(5, WireType.Fixed32);
			state.WriteInt32(amount);
		}
		Dictionary<int, Guid> byId = value.ById;
		if (byId != null)
		{
			Dictionary<int, Guid> values2 = byId;
			MapSerializer.CreateDictionary<int, Guid>().WriteMap(ref state, 6, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values2, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString, null, TypeModel.GetInbuiltSerializer<Guid>(CompatibilityLevel.Level300));
		}
		List<Guid?> certs = value.Certs;
		if (certs != null)
		{
			List<Guid?> values3 = certs;
			RepeatedSerializer.CreateList<Guid?>().WriteRepeated(ref state, 7, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values3, TypeModel.GetInbuiltSerializer<Guid?>(CompatibilityLevel.Level300, DataFormat.FixedSize));
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Payment>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<TimestampPromotion>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	TimestampPromotion ISerializer<TimestampPromotion>.Read(ref ProtoReader.State state, TimestampPromotion value)
	{
		if (value == null)
		{
			TimestampPromotion timestampPromotion = new TimestampPromotion();
			value = timestampPromotion;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				DateTime when = BclHelpers.ReadTimestamp(ref state);
				value.When = when;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<TimestampPromotion>.Write(ref ProtoWriter.State state, TimestampPromotion value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		DateTime when = value.When;
		state.WriteFieldHeader(1, WireType.String);
		DateTime value2 = when;
		BclHelpers.WriteTimestamp(ref state, value2);
	}
}
public sealed class FormatDefaultModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___FormatDefaultModel, T>();
	}
}
