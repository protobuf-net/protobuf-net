using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using AotFixtures.Exotic;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

[assembly: AssemblyVersion("0.0.0.0")]
internal sealed class ___PBN_Services___ExoticModel : ISerializer<Exotics>
{
	Exotics ISerializer<Exotics>.Read(ref ProtoReader.State state, Exotics value)
	{
		if (value == null)
		{
			Exotics exotics = new Exotics();
			value = exotics;
		}
		int num;
		while ((num = state.ReadFieldHeader()) > 0)
		{
			switch (num)
			{
			case 1:
			{
				IList<int> values = value.Interface;
				values = RepeatedSerializer.CreateEnumerable<IList<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
				if (values != null)
				{
					value.Interface = values;
				}
				break;
			}
			case 2:
			{
				ICollection<int> collection = value.Collection;
				collection = RepeatedSerializer.CreateEnumerable<ICollection<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, collection);
				if (collection != null)
				{
					value.Collection = collection;
				}
				break;
			}
			case 3:
			{
				IEnumerable<int> enumerable = value.Enumerable;
				enumerable = RepeatedSerializer.CreateEnumerable<IEnumerable<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, enumerable);
				if (enumerable != null)
				{
					value.Enumerable = enumerable;
				}
				break;
			}
			case 4:
			{
				IReadOnlyList<int> readOnlyList = value.ReadOnlyList;
				readOnlyList = RepeatedSerializer.CreateEnumerable<IReadOnlyList<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, readOnlyList);
				if (readOnlyList != null)
				{
					value.ReadOnlyList = readOnlyList;
				}
				break;
			}
			case 5:
			{
				HashSet<int> set = value.Set;
				set = RepeatedSerializer.CreateSet<HashSet<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, set);
				if (set != null)
				{
					value.Set = set;
				}
				break;
			}
			case 6:
			{
				Queue<int> queue = value.Queue;
				queue = RepeatedSerializer.CreateQueue<Queue<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, queue);
				if (queue != null)
				{
					value.Queue = queue;
				}
				break;
			}
			case 7:
			{
				Stack<int> stack = value.Stack;
				stack = RepeatedSerializer.CreateStack<Stack<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, stack);
				if (stack != null)
				{
					value.Stack = stack;
				}
				break;
			}
			case 8:
			{
				ImmutableArray<int> immutableArray = value.ImmutableArray;
				immutableArray = RepeatedSerializer.CreateImmutableArray<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, immutableArray);
				value.ImmutableArray = immutableArray;
				break;
			}
			case 9:
			{
				ImmutableList<int> immutableList = value.ImmutableList;
				immutableList = RepeatedSerializer.CreateImmutableList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, immutableList);
				if (immutableList != null)
				{
					value.ImmutableList = immutableList;
				}
				break;
			}
			case 10:
			{
				IImmutableList<int> immutableInterface = value.ImmutableInterface;
				immutableInterface = RepeatedSerializer.CreateImmutableIList<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, immutableInterface);
				if (immutableInterface != null)
				{
					value.ImmutableInterface = immutableInterface;
				}
				break;
			}
			case 11:
			{
				ConcurrentQueue<int> concurrentQueue = value.ConcurrentQueue;
				concurrentQueue = RepeatedSerializer.CreateConcurrentQueue<ConcurrentQueue<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, concurrentQueue);
				if (concurrentQueue != null)
				{
					value.ConcurrentQueue = concurrentQueue;
				}
				break;
			}
			case 12:
			{
				ConcurrentBag<int> concurrentBag = value.ConcurrentBag;
				concurrentBag = RepeatedSerializer.CreateConcurrentBag<ConcurrentBag<int>, int>().ReadRepeated(ref state, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, concurrentBag);
				if (concurrentBag != null)
				{
					value.ConcurrentBag = concurrentBag;
				}
				break;
			}
			case 13:
			{
				IList<string> strings = value.Strings;
				strings = RepeatedSerializer.CreateEnumerable<IList<string>, string>().ReadRepeated(ref state, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, strings);
				if (strings != null)
				{
					value.Strings = strings;
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

	void ISerializer<Exotics>.Write(ref ProtoWriter.State state, Exotics value)
	{
		TypeModel.ThrowUnexpectedSubtype(value);
		IList<int> list = value.Interface;
		if (list != null)
		{
			IList<int> values = list;
			RepeatedSerializer.CreateEnumerable<IList<int>, int>().WriteRepeated(ref state, 1, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values);
		}
		ICollection<int> collection = value.Collection;
		if (collection != null)
		{
			ICollection<int> values2 = collection;
			RepeatedSerializer.CreateEnumerable<ICollection<int>, int>().WriteRepeated(ref state, 2, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values2);
		}
		IEnumerable<int> enumerable = value.Enumerable;
		if (enumerable != null)
		{
			IEnumerable<int> values3 = enumerable;
			RepeatedSerializer.CreateEnumerable<IEnumerable<int>, int>().WriteRepeated(ref state, 3, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values3);
		}
		IReadOnlyList<int> readOnlyList = value.ReadOnlyList;
		if (readOnlyList != null)
		{
			IReadOnlyList<int> values4 = readOnlyList;
			RepeatedSerializer.CreateEnumerable<IReadOnlyList<int>, int>().WriteRepeated(ref state, 4, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values4);
		}
		HashSet<int> set = value.Set;
		if (set != null)
		{
			HashSet<int> values5 = set;
			RepeatedSerializer.CreateSet<HashSet<int>, int>().WriteRepeated(ref state, 5, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values5);
		}
		Queue<int> queue = value.Queue;
		if (queue != null)
		{
			Queue<int> values6 = queue;
			RepeatedSerializer.CreateQueue<Queue<int>, int>().WriteRepeated(ref state, 6, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values6);
		}
		Stack<int> stack = value.Stack;
		if (stack != null)
		{
			Stack<int> values7 = stack;
			RepeatedSerializer.CreateStack<Stack<int>, int>().WriteRepeated(ref state, 7, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values7);
		}
		ImmutableArray<int> immutableArray = value.ImmutableArray;
		RepeatedSerializer.CreateImmutableArray<int>().WriteRepeated(ref state, 8, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, immutableArray);
		ImmutableList<int> immutableList = value.ImmutableList;
		if (immutableList != null)
		{
			ImmutableList<int> values8 = immutableList;
			RepeatedSerializer.CreateImmutableList<int>().WriteRepeated(ref state, 9, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values8);
		}
		IImmutableList<int> immutableInterface = value.ImmutableInterface;
		if (immutableInterface != null)
		{
			IImmutableList<int> values9 = immutableInterface;
			RepeatedSerializer.CreateImmutableIList<int>().WriteRepeated(ref state, 10, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values9);
		}
		ConcurrentQueue<int> concurrentQueue = value.ConcurrentQueue;
		if (concurrentQueue != null)
		{
			ConcurrentQueue<int> values10 = concurrentQueue;
			RepeatedSerializer.CreateConcurrentQueue<ConcurrentQueue<int>, int>().WriteRepeated(ref state, 11, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values10);
		}
		ConcurrentBag<int> concurrentBag = value.ConcurrentBag;
		if (concurrentBag != null)
		{
			ConcurrentBag<int> values11 = concurrentBag;
			RepeatedSerializer.CreateConcurrentBag<ConcurrentBag<int>, int>().WriteRepeated(ref state, 12, SerializerFeatures.WireTypeVarint | SerializerFeatures.OptionPackedDisabled, values11);
		}
		IList<string> strings = value.Strings;
		if (strings != null)
		{
			IList<string> values12 = strings;
			RepeatedSerializer.CreateEnumerable<IList<string>, string>().WriteRepeated(ref state, 13, SerializerFeatures.WireTypeString | SerializerFeatures.OptionPackedDisabled, values12);
		}
	}

	private SerializerFeatures Features_82()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
	}

	SerializerFeatures ISerializer<Exotics>.get_Features()
	{
		//ILSpy generated this explicit interface implementation from .override directive in Features_82
		return this.Features_82();
	}
}
public sealed class ExoticModel : TypeModel
{
	protected sealed override ISerializer<T> GetSerializer<T>()
	{
		//Error decoding local variables: Signature type sequence must have at least one element.
		return SerializerCache.Get<___PBN_Services___ExoticModel, T>();
	}
}
