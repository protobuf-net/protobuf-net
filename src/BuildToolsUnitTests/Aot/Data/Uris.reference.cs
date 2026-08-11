using System;
using System.Collections.Generic;
using System.Reflection;
using AotFixtures.Uris;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___UrisModel : ISerializer<Links>
{
	Links ISerializer<Links>.Read(ref ProtoReader.State state, Links value)
	{
		if (value == null)
		{
			Links links = new Links();
			value = links;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				string text4 = state.ReadString();
				Uri uri = ((text4.Length != 0) ? new Uri(text4, UriKind.RelativeOrAbsolute) : null);
				if ((object)uri != null)
				{
					value.Home = uri;
				}
				break;
			}
			case 2:
			{
				string text2 = state.ReadString();
				if (text2 != null)
				{
					value.Name = text2;
				}
				break;
			}
			case 3:
			{
				string text3 = state.ReadString();
				Uri uri = ((text3.Length != 0) ? new Uri(text3, UriKind.RelativeOrAbsolute) : null);
				if ((object)uri != null)
				{
					value.Relative = uri;
				}
				break;
			}
			case 4:
			{
				List<Uri> all = value.All;
				all = RepeatedSerializer.CreateList<Uri>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, all);
				if (all != null)
				{
					value.All = all;
				}
				break;
			}
			case 5:
			{
				Uri[] more = value.More;
				more = RepeatedSerializer.CreateVector<Uri>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, more);
				if (more != null)
				{
					value.More = more;
				}
				break;
			}
			case 6:
			{
				Dictionary<int, Uri> byId = value.ById;
				byId = MapSerializer.CreateDictionary<int, Uri>().ReadMap(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, byId, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString);
				if (byId != null)
				{
					value.ById = byId;
				}
				break;
			}
			case 7:
			{
				string text = state.ReadString();
				if (text.Length != 0)
				{
					new Uri(text, UriKind.RelativeOrAbsolute);
				}
				else
					_ = null;
				break;
			}
			default:
				state.SkipField();
				break;
			}
		}
		return value;
	}

	void ISerializer<Links>.Write(ref ProtoWriter.State state, Links value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		Uri home = value.Home;
		string originalString;
		if ((object)home != null)
		{
			originalString = home.OriginalString;
			state.WriteString(1, originalString);
		}
		originalString = value.Name;
		state.WriteString(2, originalString);
		Uri relative = value.Relative;
		if ((object)relative != null)
		{
			originalString = relative.OriginalString;
			state.WriteString(3, originalString);
		}
		List<Uri> all = value.All;
		if (all != null)
		{
			List<Uri> values = all;
			RepeatedSerializer.CreateList<Uri>().WriteRepeated(ref state, 4, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values);
		}
		Uri[] more = value.More;
		if (more != null)
		{
			Uri[] values2 = more;
			RepeatedSerializer.CreateVector<Uri>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values2);
		}
		Dictionary<int, Uri> byId = value.ById;
		if (byId != null)
		{
			Dictionary<int, Uri> values3 = byId;
			MapSerializer.CreateDictionary<int, Uri>().WriteMap(ref state, 6, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values3, SerializerFeatures.WireTypeVarint, SerializerFeatures.WireTypeString);
		}
		Uri uri = value.Fixed;
		if ((object)uri != null)
		{
			originalString = uri.OriginalString;
			state.WriteString(7, originalString);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Links>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class UrisModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___UrisModel, T>();
	}
}
