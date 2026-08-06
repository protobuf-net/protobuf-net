using System.Reflection;
using AotFixtures.ImplicitIgnore;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ImplicitIgnoreModel : ISerializer<Excluded>, ISerializer<PartiallyPinned>
{
	Excluded ISerializer<Excluded>.Read(ref ProtoReader.State state, Excluded value)
	{
		if (value == null)
		{
			Excluded excluded = new Excluded();
			value = excluded;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int implicitProperty = state.ReadInt32();
				value.Pinned = implicitProperty;
				break;
			}
			case 4:
			{
				int implicitProperty = state.ReadInt32();
				value.ImplicitField = implicitProperty;
				break;
			}
			case 5:
			{
				int implicitProperty = state.ReadInt32();
				value.ImplicitProperty = implicitProperty;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Excluded>.Write(ref ProtoWriter.State state, Excluded value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int pinned = value.Pinned;
		if (pinned != 0)
		{
			state.WriteInt32Varint(1, pinned);
		}
		pinned = value.ImplicitField;
		if (pinned != 0)
		{
			state.WriteInt32Varint(4, pinned);
		}
		pinned = value.ImplicitProperty;
		if (pinned != 0)
		{
			state.WriteInt32Varint(5, pinned);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Excluded>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<PartiallyPinned>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	PartiallyPinned ISerializer<PartiallyPinned>.Read(ref ProtoReader.State state, PartiallyPinned value)
	{
		if (value == null)
		{
			PartiallyPinned partiallyPinned = new PartiallyPinned();
			value = partiallyPinned;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 2:
			{
				int gamma = state.ReadInt32();
				value.Beta = gamma;
				break;
			}
			case 10:
			{
				int gamma = state.ReadInt32();
				value.Alpha = gamma;
				break;
			}
			case 11:
			{
				int gamma = state.ReadInt32();
				value.Gamma = gamma;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<PartiallyPinned>.Write(ref ProtoWriter.State state, PartiallyPinned value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int beta = value.Beta;
		if (beta != 0)
		{
			state.WriteInt32Varint(2, beta);
		}
		beta = value.Alpha;
		if (beta != 0)
		{
			state.WriteInt32Varint(10, beta);
		}
		beta = value.Gamma;
		if (beta != 0)
		{
			state.WriteInt32Varint(11, beta);
		}
	}
}
public sealed class ImplicitIgnoreModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ImplicitIgnoreModel, T>();
	}
}
