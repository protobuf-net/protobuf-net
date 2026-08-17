using System.Reflection;
using AotFixtures.ConditionalDefault;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ConditionalDefaultModel : ISerializer<ConditionalDefault>
{
	ConditionalDefault ISerializer<ConditionalDefault>.Read(ref ProtoReader.State state, ConditionalDefault value)
	{
		if (value == null)
		{
			ConditionalDefault conditionalDefault = new ConditionalDefault();
			value = conditionalDefault;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Text = text;
				}
				break;
			}
			case 2:
			{
				int bare = state.ReadInt32();
				value.Number = bare;
				value.NumberSpecified = true;
				break;
			}
			case 3:
			{
				int? wrapped = state.ReadInt32();
				value.Wrapped = wrapped;
				value.WrappedSpecified = true;
				break;
			}
			case 4:
			{
				string text = state.ReadString();
				if (text != null)
				{
					value.Plain = text;
				}
				break;
			}
			case 5:
			{
				int bare = state.ReadInt32();
				value.Bare = bare;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<ConditionalDefault>.Write(ref ProtoWriter.State state, ConditionalDefault value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		if (value.ShouldSerializeText())
		{
			string text = value.Text;
			state.WriteString(1, text);
		}
		int number;
		if (value.NumberSpecified)
		{
			number = value.Number;
			state.WriteInt32Varint(2, number);
		}
		if (value.WrappedSpecified)
		{
			int? wrapped = value.Wrapped;
			if (wrapped.HasValue)
			{
				number = wrapped.GetValueOrDefault();
				state.WriteInt32Varint(3, number);
			}
		}
		string plain = value.Plain;
		if (plain != null)
		{
			string text = plain;
			if (!(text == "xyz"))
			{
				state.WriteString(4, text);
			}
		}
		number = value.Bare;
		if (number != 9)
		{
			state.WriteInt32Varint(5, number);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<ConditionalDefault>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class ConditionalDefaultModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ConditionalDefaultModel, T>();
	}
}
