using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using AotFixtures.ModelSurrogate;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: InternalsVisibleTo("System, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("System.Core, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("System.Numerics, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("System.Reflection.Context, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("System.Runtime.WindowsRuntime, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("System.Runtime.WindowsRuntime.UI.Xaml, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("WindowsBase, PublicKey=0024000004800000940000000602000000240000525341310004000001000100B5FC90E7027F67871E773A8FDE8938C81DD402BA65B9201D60593E96C492651E889CC13F1415EBB53FAC1131AE0BD333C5EE6021672D9718EA31A8AEBD0DA0072F25D87DBA6FC90FFD598ED4DA35E44C398C454307E8E33B8426143DAEC9F596836F97C8F74750E5975C64E2189F45DEF46B2A2B1247ADC3652BF5C308055DA9")]
[assembly: InternalsVisibleTo("PresentationCore, PublicKey=0024000004800000940000000602000000240000525341310004000001000100B5FC90E7027F67871E773A8FDE8938C81DD402BA65B9201D60593E96C492651E889CC13F1415EBB53FAC1131AE0BD333C5EE6021672D9718EA31A8AEBD0DA0072F25D87DBA6FC90FFD598ED4DA35E44C398C454307E8E33B8426143DAEC9F596836F97C8F74750E5975C64E2189F45DEF46B2A2B1247ADC3652BF5C308055DA9")]
[assembly: InternalsVisibleTo("PresentationFramework, PublicKey=0024000004800000940000000602000000240000525341310004000001000100B5FC90E7027F67871E773A8FDE8938C81DD402BA65B9201D60593E96C492651E889CC13F1415EBB53FAC1131AE0BD333C5EE6021672D9718EA31A8AEBD0DA0072F25D87DBA6FC90FFD598ED4DA35E44C398C454307E8E33B8426143DAEC9F596836F97C8F74750E5975C64E2189F45DEF46B2A2B1247ADC3652BF5C308055DA9")]
[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ModelSurrogateModel : ISerializer<Version>, ISerializer<Ticks>, ISerializer<Holder>, ISerializer<VersionSurrogate>, ISerializer<TicksSurrogate>
{
	Version ISerializer<Version>.Read(ref ProtoReader.State state, Version value)
	{
		VersionSurrogate versionSurrogate = value;
		if (versionSurrogate == null)
		{
			VersionSurrogate versionSurrogate2 = new VersionSurrogate();
			versionSurrogate = versionSurrogate2;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				string text = state.ReadString();
				if (text != null)
				{
					versionSurrogate.Value = text;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		value = versionSurrogate;
		return value;
	}

	void ISerializer<Version>.Write(ref ProtoWriter.State state, Version value)
	{
		VersionSurrogate versionSurrogate = value;
		TypeModel.ThrowUnexpectedSubtype(versionSurrogate);
		string value2 = versionSurrogate.Value;
		state.WriteString(1, value2);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Version>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Ticks>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Holder>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<VersionSurrogate>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<TicksSurrogate>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Ticks ISerializer<Ticks>.Read(ref ProtoReader.State state, Ticks value)
	{
		TicksSurrogate ticksSurrogate = TicksConverter.ToSurrogate(value);
		if (ticksSurrogate == null)
		{
			TicksSurrogate ticksSurrogate2 = new TicksSurrogate();
			ticksSurrogate = ticksSurrogate2;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				long value2 = state.ReadInt64();
				ticksSurrogate.Value = value2;
			}
			else
			{
				state.SkipField();
			}
		}
		value = TicksConverter.FromSurrogate(ticksSurrogate);
		return value;
	}

	void ISerializer<Ticks>.Write(ref ProtoWriter.State state, Ticks value)
	{
		TicksSurrogate ticksSurrogate = TicksConverter.ToSurrogate(value);
		TypeModel.ThrowUnexpectedSubtype(ticksSurrogate);
		long value2 = ticksSurrogate.Value;
		if (value2 != 0L)
		{
			state.WriteFieldHeader(1, WireType.Variant);
			state.WriteInt64(value2);
		}
	}

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
			switch (num)
			{
			case 1:
			{
				Version release = value.Release;
				release = state.ReadMessage(SerializerFeatures.CategoryRepeated, release, this);
				if ((object)release != null)
				{
					value.Release = release;
				}
				break;
			}
			case 2:
			{
				Ticks elapsed = value.Elapsed;
				elapsed = state.ReadMessage(SerializerFeatures.CategoryRepeated, elapsed, this);
				value.Elapsed = elapsed;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Holder>.Write(ref ProtoWriter.State state, Holder value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Version release = value.Release;
		state.WriteMessage(1, SerializerFeatures.CategoryRepeated, release, this);
		Ticks elapsed = value.Elapsed;
		state.WriteMessage(2, SerializerFeatures.CategoryRepeated, elapsed, this);
	}

	VersionSurrogate ISerializer<VersionSurrogate>.Read(ref ProtoReader.State state, VersionSurrogate value)
	{
		if (value == null)
		{
			VersionSurrogate versionSurrogate = new VersionSurrogate();
			value = versionSurrogate;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Value = text;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<VersionSurrogate>.Write(ref ProtoWriter.State state, VersionSurrogate value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		string value2 = value.Value;
		state.WriteString(1, value2);
	}

	TicksSurrogate ISerializer<TicksSurrogate>.Read(ref ProtoReader.State state, TicksSurrogate value)
	{
		if (value == null)
		{
			TicksSurrogate ticksSurrogate = new TicksSurrogate();
			value = ticksSurrogate;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				long value2 = state.ReadInt64();
				value.Value = value2;
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<TicksSurrogate>.Write(ref ProtoWriter.State state, TicksSurrogate value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		long value2 = value.Value;
		if (value2 != 0L)
		{
			state.WriteFieldHeader(1, WireType.Variant);
			state.WriteInt64(value2);
		}
	}
}
public sealed class ModelSurrogateModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ModelSurrogateModel, T>();
	}
}
