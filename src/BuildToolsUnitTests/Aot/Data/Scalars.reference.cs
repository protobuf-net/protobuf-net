using System.Reflection;
using AotFixtures.Scalars;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ScalarsModel : ISerializer<Primitives>
{
	Primitives ISerializer<Primitives>.Read(ref ProtoReader.State state, Primitives value)
	{
		if (value == null)
		{
			Primitives primitives = new Primitives();
			value = primitives;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				bool flag = state.ReadBoolean();
				value.Bool = flag;
				break;
			}
			case 2:
			{
				sbyte sByte = state.ReadSByte();
				value.SByte = sByte;
				break;
			}
			case 3:
			{
				byte b = state.ReadByte();
				value.Byte = b;
				break;
			}
			case 4:
			{
				short int3 = state.ReadInt16();
				value.Int16 = int3;
				break;
			}
			case 5:
			{
				ushort uInt3 = state.ReadUInt16();
				value.UInt16 = uInt3;
				break;
			}
			case 6:
			{
				int int2 = state.ReadInt32();
				value.Int32 = int2;
				break;
			}
			case 7:
			{
				uint uInt2 = state.ReadUInt32();
				value.UInt32 = uInt2;
				break;
			}
			case 8:
			{
				long @int = state.ReadInt64();
				value.Int64 = @int;
				break;
			}
			case 9:
			{
				ulong uInt = state.ReadUInt64();
				value.UInt64 = uInt;
				break;
			}
			case 10:
			{
				float single = state.ReadSingle();
				value.Single = single;
				break;
			}
			case 11:
			{
				double num2 = state.ReadDouble();
				value.Double = num2;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Primitives>.Write(ref ProtoWriter.State state, Primitives value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		bool flag = value.Bool;
		if (flag)
		{
			state.WriteFieldHeader(1, WireType.Variant);
			state.WriteBoolean(flag);
		}
		sbyte sByte = value.SByte;
		if (sByte != 0)
		{
			state.WriteFieldHeader(2, WireType.Variant);
			state.WriteSByte(sByte);
		}
		byte b = value.Byte;
		if (b != 0)
		{
			state.WriteFieldHeader(3, WireType.Variant);
			state.WriteByte(b);
		}
		short @int = value.Int16;
		if (@int != 0)
		{
			state.WriteFieldHeader(4, WireType.Variant);
			state.WriteInt16(@int);
		}
		ushort uInt = value.UInt16;
		if (uInt != 0)
		{
			state.WriteFieldHeader(5, WireType.Variant);
			state.WriteUInt16(uInt);
		}
		int int2 = value.Int32;
		if (int2 != 0)
		{
			state.WriteInt32Varint(6, int2);
		}
		uint uInt2 = value.UInt32;
		if (uInt2 != 0)
		{
			state.WriteFieldHeader(7, WireType.Variant);
			state.WriteUInt32(uInt2);
		}
		long int3 = value.Int64;
		if (int3 != 0L)
		{
			state.WriteFieldHeader(8, WireType.Variant);
			state.WriteInt64(int3);
		}
		ulong uInt3 = value.UInt64;
		if (uInt3 != 0L)
		{
			state.WriteFieldHeader(9, WireType.Variant);
			state.WriteUInt64(uInt3);
		}
		float single = value.Single;
		if (single != 0f)
		{
			state.WriteFieldHeader(10, WireType.Fixed32);
			state.WriteSingle(single);
		}
		double num = value.Double;
		if (num != 0.0)
		{
			state.WriteFieldHeader(11, WireType.Fixed64);
			state.WriteDouble(num);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Primitives>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class ScalarsModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ScalarsModel, T>();
	}
}
