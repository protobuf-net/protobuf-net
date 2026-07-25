using System.Reflection;
using System.Runtime.CompilerServices;
using AotFixtures.ExternalSerializer;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ExternalSerializerModel : ISerializer<Holder>, ISerializerProxy<Thing>
{
	Holder ISerializer<Holder>.Read(ref ProtoReader.State state, Holder value)
	{
		if (value == null)
		{
			Holder holder = new Holder();
			value = holder;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				Thing thing = value.Thing;
				thing = state.ReadMessage(SerializerFeatures.CategoryRepeated, thing, SerializerCache.Get<ThingSerializer, Thing>());
				if (thing != null)
				{
					value.Thing = thing;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Holder>.Write(ref ProtoWriter.State state, Holder value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Thing thing = value.Thing;
		state.WriteMessage(1, SerializerFeatures.CategoryRepeated, thing, SerializerCache.Get<ThingSerializer, Thing>());
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Holder>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	[SpecialName]
	ISerializer<Thing> ISerializerProxy<Thing>.get_Serializer()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<ThingSerializer, Thing>();
	}
}
public sealed class ExternalSerializerModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ExternalSerializerModel, T>();
	}
}
