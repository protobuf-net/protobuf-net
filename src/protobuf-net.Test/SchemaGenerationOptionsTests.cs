using ProtoBuf.Meta;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ProtoBuf
{
    public class SchemaGenerationOptionsTests
    {
        [Fact]
        public void CopyConstructorCopiesEverySettableProperty()
        {
            var source = new SchemaGenerationOptions
            {
                Syntax = ProtoSyntax.Proto2,
                Flags = SchemaGenerationFlags.PreserveSubType | SchemaGenerationFlags.MultipleNamespaceSupport,
                Package = "some.package",
                Origin = "some/origin.proto",
            };

            var clone = new SchemaGenerationOptions(source);

            Assert.Equal(source.Syntax, clone.Syntax);
            Assert.Equal(source.Flags, clone.Flags);
            Assert.Equal(source.Package, clone.Package);
            Assert.Equal(source.Origin, clone.Origin);
        }

        /// <summary>
        /// Fails when a property is added to <see cref="SchemaGenerationOptions"/> without being
        /// taught to the copy constructor.
        /// </summary>
        /// <remarks>
        /// The point of the copy constructor is that callers stop hand-rolling an incomplete copy; it
        /// would be poor if the copy constructor itself then became the incomplete one, silently, with
        /// nothing failing. So this asserts by reflection rather than by an enumerated list.
        /// </remarks>
        [Fact]
        public void CopyConstructorHandlesEveryPublicProperty()
        {
            var properties = typeof(SchemaGenerationOptions)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var source = new SchemaGenerationOptions();
            var untouched = new SchemaGenerationOptions();

            // give every settable property a value that differs from the default
            foreach (var property in properties.Where(static x => x.GetSetMethod() is not null))
            {
                property.SetValue(source, DistinctValue(property, property.GetValue(untouched)));
            }
            // ...and put something in every collection
            source.Services.Add(new Service { Name = "SomeService" });
            source.Types.Add(typeof(SchemaGenerationOptionsTests));

            var clone = new SchemaGenerationOptions(source);

            foreach (var property in properties)
            {
                var expected = property.GetValue(source);
                var actual = property.GetValue(clone);

                if (expected is System.Collections.ICollection expectedItems)
                {
                    var actualItems = Assert.IsAssignableFrom<System.Collections.ICollection>(actual);
                    Assert.True(expectedItems.Count == actualItems.Count,
                        $"{property.Name} was not copied ({expectedItems.Count} vs {actualItems.Count} items)");
                    Assert.True(expectedItems.Cast<object>().SequenceEqual(actualItems.Cast<object>()),
                        $"{property.Name} contents differ");
                    Assert.False(ReferenceEquals(expected, actual),
                        $"{property.Name} is shared by reference; the two instances must be independently mutable");
                }
                else
                {
                    Assert.True(Equals(expected, actual), $"{property.Name} was not copied");
                }
            }

            static object DistinctValue(PropertyInfo property, object current)
            {
                var type = property.PropertyType;
                if (type == typeof(string)) return property.Name + "-value";
                if (type.IsEnum)
                {
                    // any value other than whatever the default happens to be
                    return Enum.GetValues(type).Cast<object>().First(x => !Equals(x, current));
                }
                if (type == typeof(bool)) return !(bool)current;
                if (type == typeof(int)) return (int)current + 1;
                throw new NotSupportedException(
                    $"{property.Name} is a {type.Name}; teach this test how to vary it, and check the copy constructor handles it");
            }
        }

        [Fact]
        public void CopyingAnUntouchedInstanceDoesNotAllocateTheCollections()
        {
            // laziness is worth preserving: SchemaGenerationOptions.Default exists and is copied often
            var clone = new SchemaGenerationOptions(new SchemaGenerationOptions());
            Assert.Empty(clone.Services);
            Assert.Empty(clone.Types);
        }

        [Fact]
        public void CollectionsAreIndependentAfterCopying()
        {
            var source = new SchemaGenerationOptions();
            source.Services.Add(new Service { Name = "First" });

            var clone = new SchemaGenerationOptions(source);
            clone.Services.Add(new Service { Name = "Second" });

            Assert.Single(source.Services);
            Assert.Equal(2, clone.Services.Count);
        }

        [Fact]
        public void CopyingNullThrows()
            => Assert.Throws<ArgumentNullException>(static () => new SchemaGenerationOptions(null));
    }
}
