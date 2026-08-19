using System;
using System.Reflection;
using AotFixtures.BclLevel300;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___BclLevel300Model : ISerializer<Level300>
{
	Level300 ISerializer<Level300>.Read(ref ProtoReader.State state, Level300 value)
	{
		if (value == null)
		{
			Level300 level = new Level300();
			value = level;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				Guid alwaysGuid = BclHelpers.ReadGuidString(ref state);
				value.AsString = alwaysGuid;
				break;
			}
			case 2:
			{
				Guid alwaysGuid = BclHelpers.ReadGuidBytes(ref state);
				value.AsBytes = alwaysGuid;
				break;
			}
			case 3:
			{
				decimal alwaysAmount = BclHelpers.ReadDecimalString(ref state);
				value.Amount = alwaysAmount;
				break;
			}
			case 4:
			{
				Guid? maybeGuid = BclHelpers.ReadGuidString(ref state);
				value.MaybeGuid = maybeGuid;
				break;
			}
			case 5:
			{
				decimal? maybeAmount = BclHelpers.ReadDecimalString(ref state);
				value.MaybeAmount = maybeAmount;
				break;
			}
			case 6:
			{
				Guid alwaysGuid = BclHelpers.ReadGuidString(ref state);
				value.AlwaysGuid = alwaysGuid;
				break;
			}
			case 7:
			{
				decimal alwaysAmount = BclHelpers.ReadDecimalString(ref state);
				value.AlwaysAmount = alwaysAmount;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Level300>.Write(ref ProtoWriter.State state, Level300 value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Guid asString = value.AsString;
		if (!(asString == Guid.Empty))
		{
			state.WriteFieldHeader(1, WireType.String);
			BclHelpers.WriteGuidString(ref state, asString);
		}
		asString = value.AsBytes;
		if (!(asString == Guid.Empty))
		{
			state.WriteFieldHeader(2, WireType.String);
			BclHelpers.WriteGuidBytes(ref state, asString);
		}
		decimal amount = value.Amount;
		if (!(amount == 0m))
		{
			state.WriteFieldHeader(3, WireType.String);
			BclHelpers.WriteDecimalString(ref state, amount);
		}
		Guid? maybeGuid = value.MaybeGuid;
		if (maybeGuid.HasValue)
		{
			Guid valueOrDefault = maybeGuid.GetValueOrDefault();
			state.WriteFieldHeader(4, WireType.String);
			asString = valueOrDefault;
			BclHelpers.WriteGuidString(ref state, asString);
		}
		decimal? maybeAmount = value.MaybeAmount;
		if (maybeAmount.HasValue)
		{
			decimal valueOrDefault2 = maybeAmount.GetValueOrDefault();
			state.WriteFieldHeader(5, WireType.String);
			amount = valueOrDefault2;
			BclHelpers.WriteDecimalString(ref state, amount);
		}
		Guid alwaysGuid = value.AlwaysGuid;
		state.WriteFieldHeader(6, WireType.String);
		asString = alwaysGuid;
		BclHelpers.WriteGuidString(ref state, asString);
		decimal alwaysAmount = value.AlwaysAmount;
		state.WriteFieldHeader(7, WireType.String);
		amount = alwaysAmount;
		BclHelpers.WriteDecimalString(ref state, amount);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Level300>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class BclLevel300Model : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___BclLevel300Model, T>();
	}
}
