using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Meta
{
	public class OpenGenericMetaTypeTests
	{
		public class OpenGenericTarget<T>
		{
			public T Value { get; set; }
		}

		[ProtoContract]
		public class OpenGenericSurrogate<T>
		{
			[ProtoMember(1)]
			public T Value { get; set; }


			public static implicit operator OpenGenericTarget<T>(OpenGenericSurrogate<T> surrogate)
			{
				return surrogate == null ? null : new OpenGenericTarget<T>
				{
					Value = surrogate.Value
				};
			}

			public static implicit operator OpenGenericSurrogate<T>(OpenGenericTarget<T> source)
			{
				return source == null ? null : new OpenGenericSurrogate<T>
				{
					Value = source.Value
				};
			}
		}

		[Fact]
		public void OpenGenericMetaTypeSerialization()
		{
			RuntimeTypeModel.Default.Add(typeof(OpenGenericTarget<>)).SetSurrogate(typeof(OpenGenericSurrogate<>));

			var instance = new OpenGenericTarget<string> { Value = "XYZ!" };
			var clone = RuntimeTypeModel.Default.DeepClone(instance);

			Assert.Equal(instance.Value, clone.Value);
		}
	}
}
