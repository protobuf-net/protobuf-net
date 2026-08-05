using System.Collections.Generic;
using System.Reflection;
using AotFixtures.InterfaceMembers;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___InterfaceMembersModel : ISerializer<Holder>, ISerializer<IRoot>, ISubTypeSerializer<IRoot>, ISerializer<IMiddle>, ISubTypeSerializer<IMiddle>, ISerializer<Leaf>, ISubTypeSerializer<Leaf>, ISerializer<IBox<int>>, ISubTypeSerializer<IBox<int>>, ISerializer<Box>, ISubTypeSerializer<Box>, ISerializer<INameable>, ISubTypeSerializer<INameable>, ISerializer<Named>, ISubTypeSerializer<Named>
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
			switch (num)
			{
			case 1:
			{
				IRoot viaRoot = value.ViaRoot;
				viaRoot = state.ReadMessage(SerializerFeatures.CategoryRepeated, viaRoot, this);
				if (viaRoot != null)
				{
					value.ViaRoot = viaRoot;
				}
				break;
			}
			case 2:
			{
				IMiddle viaMiddle = value.ViaMiddle;
				viaMiddle = state.ReadMessage(SerializerFeatures.CategoryRepeated, viaMiddle, this);
				if (viaMiddle != null)
				{
					value.ViaMiddle = viaMiddle;
				}
				break;
			}
			case 3:
			{
				IBox<int> boxed = value.Boxed;
				boxed = state.ReadMessage(SerializerFeatures.CategoryRepeated, boxed, this);
				if (boxed != null)
				{
					value.Boxed = boxed;
				}
				break;
			}
			case 6:
			{
				Dictionary<int, INameable> byIndex = value.ByIndex;
				byIndex = MapSerializer.CreateDictionary<int, INameable>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, byIndex, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString, null, this);
				if (byIndex != null)
				{
					value.ByIndex = byIndex;
				}
				break;
			}
			case 7:
			{
				Dictionary<INameable, int> byName = value.ByName;
				byName = MapSerializer.CreateDictionary<INameable, int>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, byName, SerializerFeatures.WireTypeString, SerializerFeatures.WireTypeVarint, this);
				if (byName != null)
				{
					value.ByName = byName;
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

	void ISerializer<Holder>.Write(ref ProtoWriter.State state, Holder value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		IRoot viaRoot = value.ViaRoot;
		state.WriteMessage(1, SerializerFeatures.CategoryRepeated, viaRoot, this);
		IMiddle viaMiddle = value.ViaMiddle;
		state.WriteMessage(2, SerializerFeatures.CategoryRepeated, viaMiddle, this);
		IBox<int> boxed = value.Boxed;
		state.WriteMessage(3, SerializerFeatures.CategoryRepeated, boxed, this);
		Dictionary<int, INameable> byIndex = value.ByIndex;
		if (byIndex != null)
		{
			Dictionary<int, INameable> values = byIndex;
			MapSerializer.CreateDictionary<int, INameable>().WriteMap(ref state, 6, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString, null, this);
		}
		Dictionary<INameable, int> byName = value.ByName;
		if (byName != null)
		{
			Dictionary<INameable, int> values2 = byName;
			MapSerializer.CreateDictionary<INameable, int>().WriteMap(ref state, 7, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled | SerializerFeatures.OptionFailOnDuplicateKey, values2, SerializerFeatures.WireTypeString, SerializerFeatures.WireTypeVarint, this);
		}
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

	SerializerFeatures ISerializer<IRoot>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<IMiddle>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Leaf>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<IBox<int>>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Box>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<INameable>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Named>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	IRoot ISerializer<IRoot>.Read(ref ProtoReader.State state, IRoot value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return ((ISubTypeSerializer<IRoot>)this).ReadSubType(ref state, SubTypeState<IRoot>.Create(state.Context, value));
	}

	void ISerializer<IRoot>.Write(ref ProtoWriter.State state, IRoot value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<IRoot>)this).WriteSubType(ref state, value);
	}

	void ISubTypeSerializer<IRoot>.WriteSubType(ref ProtoWriter.State state, IRoot value)
	{
		if (TypeModel.IsSubType(value))
		{
			if (value is IMiddle value2)
			{
				state.WriteSubType(10, value2, this);
			}
			else
			{
				TypeModel.ThrowUnexpectedSubtype(value);
			}
		}
	}

	IRoot ISubTypeSerializer<IRoot>.ReadSubType(ref ProtoReader.State state, SubTypeState<IRoot> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 10)
			{
				value.ReadSubType<IMiddle>(ref state, this);
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	IMiddle ISerializer<IMiddle>.Read(ref ProtoReader.State state, IMiddle value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (IMiddle)((ISubTypeSerializer<IRoot>)this).ReadSubType(ref state, SubTypeState<IRoot>.Create(state.Context, value));
	}

	void ISerializer<IMiddle>.Write(ref ProtoWriter.State state, IMiddle value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<IRoot>)this).WriteSubType(ref state, (IRoot)value);
	}

	void ISubTypeSerializer<IMiddle>.WriteSubType(ref ProtoWriter.State state, IMiddle value)
	{
		if (TypeModel.IsSubType(value))
		{
			if (value is Leaf value2)
			{
				state.WriteSubType(11, value2, this);
			}
			else
			{
				TypeModel.ThrowUnexpectedSubtype(value);
			}
		}
	}

	IMiddle ISubTypeSerializer<IMiddle>.ReadSubType(ref ProtoReader.State state, SubTypeState<IMiddle> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 11)
			{
				value.ReadSubType<Leaf>(ref state, this);
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	Leaf ISerializer<Leaf>.Read(ref ProtoReader.State state, Leaf value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (Leaf)((ISubTypeSerializer<IRoot>)this).ReadSubType(ref state, SubTypeState<IRoot>.Create(state.Context, value));
	}

	void ISerializer<Leaf>.Write(ref ProtoWriter.State state, Leaf value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<IRoot>)this).WriteSubType(ref state, (IRoot)value);
	}

	void ISubTypeSerializer<Leaf>.WriteSubType(ref ProtoWriter.State state, Leaf value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int n = value.N;
		if (n != 0)
		{
			state.WriteInt32Varint(1, n);
		}
	}

	Leaf ISubTypeSerializer<Leaf>.ReadSubType(ref ProtoReader.State state, SubTypeState<Leaf> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				Leaf value2 = value.Value;
				int n = state.ReadInt32();
				value2.N = n;
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	IBox<int> ISerializer<IBox<int>>.Read(ref ProtoReader.State state, IBox<int> value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return ((ISubTypeSerializer<IBox<int>>)this).ReadSubType(ref state, SubTypeState<IBox<int>>.Create(state.Context, value));
	}

	void ISerializer<IBox<int>>.Write(ref ProtoWriter.State state, IBox<int> value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<IBox<int>>)this).WriteSubType(ref state, value);
	}

	void ISubTypeSerializer<IBox<int>>.WriteSubType(ref ProtoWriter.State state, IBox<int> value)
	{
		if (TypeModel.IsSubType(value))
		{
			if (value is Box value2)
			{
				state.WriteSubType(10, value2, this);
			}
			else
			{
				TypeModel.ThrowUnexpectedSubtype(value);
			}
		}
	}

	IBox<int> ISubTypeSerializer<IBox<int>>.ReadSubType(ref ProtoReader.State state, SubTypeState<IBox<int>> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 10)
			{
				value.ReadSubType<Box>(ref state, this);
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	Box ISerializer<Box>.Read(ref ProtoReader.State state, Box value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (Box)((ISubTypeSerializer<IBox<int>>)this).ReadSubType(ref state, SubTypeState<IBox<int>>.Create(state.Context, value));
	}

	void ISerializer<Box>.Write(ref ProtoWriter.State state, Box value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<IBox<int>>)this).WriteSubType(ref state, (IBox<int>)value);
	}

	void ISubTypeSerializer<Box>.WriteSubType(ref ProtoWriter.State state, Box value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int n = value.N;
		if (n != 0)
		{
			state.WriteInt32Varint(1, n);
		}
	}

	Box ISubTypeSerializer<Box>.ReadSubType(ref ProtoReader.State state, SubTypeState<Box> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				Box value2 = value.Value;
				int n = state.ReadInt32();
				value2.N = n;
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	INameable ISerializer<INameable>.Read(ref ProtoReader.State state, INameable value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return ((ISubTypeSerializer<INameable>)this).ReadSubType(ref state, SubTypeState<INameable>.Create(state.Context, value));
	}

	void ISerializer<INameable>.Write(ref ProtoWriter.State state, INameable value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<INameable>)this).WriteSubType(ref state, value);
	}

	void ISubTypeSerializer<INameable>.WriteSubType(ref ProtoWriter.State state, INameable value)
	{
		if (TypeModel.IsSubType(value))
		{
			if (value is Named value2)
			{
				state.WriteSubType(10, value2, this);
			}
			else
			{
				TypeModel.ThrowUnexpectedSubtype(value);
			}
		}
	}

	INameable ISubTypeSerializer<INameable>.ReadSubType(ref ProtoReader.State state, SubTypeState<INameable> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 10)
			{
				value.ReadSubType<Named>(ref state, this);
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}

	Named ISerializer<Named>.Read(ref ProtoReader.State state, Named value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return (Named)((ISubTypeSerializer<INameable>)this).ReadSubType(ref state, SubTypeState<INameable>.Create(state.Context, value));
	}

	void ISerializer<Named>.Write(ref ProtoWriter.State state, Named value)
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		((ISubTypeSerializer<INameable>)this).WriteSubType(ref state, (INameable)value);
	}

	void ISubTypeSerializer<Named>.WriteSubType(ref ProtoWriter.State state, Named value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		string s = value.S;
		state.WriteString(1, s);
	}

	Named ISubTypeSerializer<Named>.ReadSubType(ref ProtoReader.State state, SubTypeState<Named> value)
	{
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				Named value2 = value.Value;
				string text = state.ReadString();
				if (text != null)
				{
					value2.S = text;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		return value.Value;
	}
}
public sealed class InterfaceMembersModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___InterfaceMembersModel, T>();
	}
}
