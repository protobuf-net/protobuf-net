using System.Reflection;
using AotFixtures.Nested;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___NestedModel : ISerializer<Invoice>, ISerializer<Customer>, ISerializer<Address>
{
	Invoice ISerializer<Invoice>.Read(ref ProtoReader.State state, Invoice value)
	{
		if (value == null)
		{
			Invoice invoice = new Invoice();
			value = invoice;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int number = state.ReadInt32();
				value.Number = number;
				break;
			}
			case 2:
			{
				Customer customer = value.Customer;
				customer = state.ReadMessage(SerializerFeatures.CategoryRepeated, customer, this);
				if (customer != null)
				{
					value.Customer = customer;
				}
				break;
			}
			case 3:
			{
				Address shipTo = value.ShipTo;
				shipTo = state.ReadMessage(SerializerFeatures.CategoryRepeated, shipTo, this);
				if (shipTo != null)
				{
					value.ShipTo = shipTo;
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

	void ISerializer<Invoice>.Write(ref ProtoWriter.State state, Invoice value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int number = value.Number;
		if (number != 0)
		{
			state.WriteInt32Varint(1, number);
		}
		Customer customer = value.Customer;
		state.WriteMessage(2, SerializerFeatures.CategoryRepeated, customer, this);
		Address shipTo = value.ShipTo;
		state.WriteMessage(3, SerializerFeatures.CategoryRepeated, shipTo, this);
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Invoice>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Customer>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	SerializerFeatures ISerializer<Address>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}

	Customer ISerializer<Customer>.Read(ref ProtoReader.State state, Customer value)
	{
		if (value == null)
		{
			Customer customer = new Customer();
			value = customer;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				int id = state.ReadInt32();
				value.Id = id;
				break;
			}
			case 2:
			{
				Address address = value.Address;
				address = state.ReadMessage(SerializerFeatures.CategoryRepeated, address, this);
				if (address != null)
				{
					value.Address = address;
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

	void ISerializer<Customer>.Write(ref ProtoWriter.State state, Customer value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		int id = value.Id;
		if (id != 0)
		{
			state.WriteInt32Varint(1, id);
		}
		Address address = value.Address;
		state.WriteMessage(2, SerializerFeatures.CategoryRepeated, address, this);
	}

	Address ISerializer<Address>.Read(ref ProtoReader.State state, Address value)
	{
		if (value == null)
		{
			Address address = new Address();
			value = address;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			if (num == 1)
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.City = text;
				}
			}
			else
			{
				state.SkipField();
			}
		}
		return value;
	}

	void ISerializer<Address>.Write(ref ProtoWriter.State state, Address value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		string city = value.City;
		state.WriteString(1, city);
	}
}
public sealed class NestedModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___NestedModel, T>();
	}
}
