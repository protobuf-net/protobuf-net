using System.Reflection;
using System.Runtime.CompilerServices;
using AotFixtures.ModelSerializer;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ModelSerializerModel : ISerializer<Request>, ISerializerProxy<Wrapped<byte>>, ISerializerProxy<Wrapped<long>>, ISerializerProxy<Wrapped<string>>, ISerializerProxy<Wrapped<int>>
{
	Request ISerializer<Request>.Read(ref ProtoReader.State state, Request value)
	{
		if (value == null)
		{
			Request request = new Request();
			value = request;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				Wrapped<byte> special = value.Special;
				special = SerializerCache.Get<WrappedByteSerializer, Wrapped<byte>>().Read(ref state, special);
				value.Special = special;
				break;
			}
			case 2:
			{
				int plain = state.ReadInt32();
				value.Plain = plain;
				break;
			}
			case 3:
			{
				Wrapped<int> id = value.Id;
				id = SerializerCache.Get<WrappedSerializer<int>, Wrapped<int>>().Read(ref state, id);
				value.Id = id;
				break;
			}
			case 4:
			{
				Wrapped<string> label = value.Label;
				label = SerializerCache.Get<WrappedSerializer<string>, Wrapped<string>>().Read(ref state, label);
				value.Label = label;
				break;
			}
			case 5:
			{
				Wrapped<long> valueOrDefault = value.Optional.GetValueOrDefault();
				Wrapped<long>? optional = SerializerCache.Get<WrappedSerializer<long>, Wrapped<long>>().Read(ref state, valueOrDefault);
				value.Optional = optional;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Request>.Write(ref ProtoWriter.State state, Request value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Wrapped<byte> special = value.Special;
		state.WriteFieldHeader(1, WireType.Fixed32);
		Wrapped<byte> value2 = special;
		SerializerCache.Get<WrappedByteSerializer, Wrapped<byte>>().Write(ref state, value2);
		int plain = value.Plain;
		if (plain != 0)
		{
			state.WriteInt32Varint(2, plain);
		}
		Wrapped<int> id = value.Id;
		state.WriteFieldHeader(3, WireType.Variant);
		Wrapped<int> value3 = id;
		SerializerCache.Get<WrappedSerializer<int>, Wrapped<int>>().Write(ref state, value3);
		Wrapped<string> label = value.Label;
		state.WriteFieldHeader(4, WireType.Variant);
		Wrapped<string> value4 = label;
		SerializerCache.Get<WrappedSerializer<string>, Wrapped<string>>().Write(ref state, value4);
		Wrapped<long>? optional = value.Optional;
		if (optional.HasValue)
		{
			Wrapped<long> valueOrDefault = optional.GetValueOrDefault();
			state.WriteFieldHeader(5, WireType.Variant);
			Wrapped<long> value5 = valueOrDefault;
			SerializerCache.Get<WrappedSerializer<long>, Wrapped<long>>().Write(ref state, value5);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Request>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	[SpecialName]
	ISerializer<Wrapped<byte>> ISerializerProxy<Wrapped<byte>>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<WrappedByteSerializer, Wrapped<byte>>();
	}

	[SpecialName]
	ISerializer<Wrapped<long>> ISerializerProxy<Wrapped<long>>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<WrappedSerializer<long>, Wrapped<long>>();
	}

	[SpecialName]
	ISerializer<Wrapped<string>> ISerializerProxy<Wrapped<string>>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<WrappedSerializer<string>, Wrapped<string>>();
	}

	[SpecialName]
	ISerializer<Wrapped<int>> ISerializerProxy<Wrapped<int>>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<WrappedSerializer<int>, Wrapped<int>>();
	}
}
public sealed class ModelSerializerModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ModelSerializerModel, T>();
	}
}
