using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using AotFixtures.Tuples;
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
internal sealed class ___PBN_Services___TuplesModel : ISerializer<ClassTuple>, ISerializer<StructTuple>, ISerializer<NamedLikeATuple>, ISerializer<KeyValuePair<int, string>>, ISerializer<(int, string)>, ISerializer<Tuple<int, string>>
{
	ClassTuple ISerializer<ClassTuple>.Read(ref ProtoReader.State state, ClassTuple value)
	{
		int a = 0;
		string b = null;
		if (value != null)
		{
			a = value.A;
			b = value.B;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
				a = state.ReadInt32();
				break;
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					b = text;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		value = new ClassTuple(a, b);
		return value;
	}

	void ISerializer<ClassTuple>.Write(ref ProtoWriter.State state, ClassTuple value)
	{
		int a = value.A;
		state.WriteInt32Varint(1, a);
		string b = value.B;
		state.WriteString(2, b);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<ClassTuple>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<StructTuple>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<NamedLikeATuple>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<KeyValuePair<int, string>>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<(int, string)>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Tuple<int, string>>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	StructTuple ISerializer<StructTuple>.Read(ref ProtoReader.State state, StructTuple value)
	{
		int x = value.X;
		string y = value.Y;
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
				x = state.ReadInt32();
				break;
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					y = text;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		value = new StructTuple(x, y);
		return value;
	}

	void ISerializer<StructTuple>.Write(ref ProtoWriter.State state, StructTuple value)
	{
		int x = value.X;
		state.WriteInt32Varint(1, x);
		string y = value.Y;
		state.WriteString(2, y);
	}

	NamedLikeATuple ISerializer<NamedLikeATuple>.Read(ref ProtoReader.State state, NamedLikeATuple value)
	{
		int first = value.First;
		int second = value.Second;
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
				first = state.ReadInt32();
				break;
			case 2:
				second = state.ReadInt32();
				break;
			default:
				state.SkipField();
				break;
			}
		}
		value = new NamedLikeATuple(first, second);
		return value;
	}

	void ISerializer<NamedLikeATuple>.Write(ref ProtoWriter.State state, NamedLikeATuple value)
	{
		int first = value.First;
		state.WriteInt32Varint(1, first);
		first = value.Second;
		state.WriteInt32Varint(2, first);
	}

	KeyValuePair<int, string> ISerializer<KeyValuePair<int, string>>.Read(ref ProtoReader.State state, KeyValuePair<int, string> value)
	{
		int key = value.Key;
		string value2 = value.Value;
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
				key = state.ReadInt32();
				break;
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value2 = text;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		value = new KeyValuePair<int, string>(key, value2);
		return value;
	}

	void ISerializer<KeyValuePair<int, string>>.Write(ref ProtoWriter.State state, KeyValuePair<int, string> value)
	{
		int key = value.Key;
		state.WriteInt32Varint(1, key);
		string value2 = value.Value;
		state.WriteString(2, value2);
	}

	(int, string) ISerializer<(int, string)>.Read(ref ProtoReader.State state, (int, string) value)
	{
		var (item, item2) = value;
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
				item = state.ReadInt32();
				break;
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					item2 = text;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		value = (item, item2);
		return value;
	}

	void ISerializer<(int, string)>.Write(ref ProtoWriter.State state, (int, string) value)
	{
		var (value2, _) = value;
		state.WriteInt32Varint(1, value2);
		string item = value.Item2;
		state.WriteString(2, item);
	}

	Tuple<int, string> ISerializer<Tuple<int, string>>.Read(ref ProtoReader.State state, Tuple<int, string> value)
	{
		int item = 0;
		string item2 = null;
		if (value != null)
		{
			item = value.Item1;
			item2 = value.Item2;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
				item = state.ReadInt32();
				break;
			case 2:
			{
				string text = state.ReadString();
				if (text != null)
				{
					item2 = text;
				}
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		value = new Tuple<int, string>(item, item2);
		return value;
	}

	void ISerializer<Tuple<int, string>>.Write(ref ProtoWriter.State state, Tuple<int, string> value)
	{
		int item = value.Item1;
		state.WriteInt32Varint(1, item);
		string item2 = value.Item2;
		state.WriteString(2, item2);
	}
}
public sealed class TuplesModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___TuplesModel, T>();
	}
}
