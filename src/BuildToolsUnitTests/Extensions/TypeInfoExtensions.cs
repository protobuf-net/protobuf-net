#nullable enable

using System;
using System.Reflection;
using FluentAssertions;

namespace BuildToolsUnitTests.Extensions
{
    internal static class TypeInfoExtensions
    {
        public static void CheckFieldType(
            this TypeInfo typeInfo,
            string fieldName,
            Type expectedType,
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            var field = typeInfo.GetField(fieldName, bindingFlags);
            field.Should().NotBeNull();
            field!.FieldType.Should().Be(expectedType);
        }
        
        public static void CheckPropertyType(
            this TypeInfo typeInfo,
            string propertyName,
            string expectedTypeName)
        {
            var property = typeInfo.GetProperty(propertyName);
            property.Should().NotBeNull();
            property!.PropertyType.FullName.Should().Be(expectedTypeName);
        } 
        
        public static void CheckPropertyType(
            this TypeInfo typeInfo,
            string propertyName,
            Type expectedType)
        {
            var property = typeInfo.GetProperty(propertyName);
            property.Should().NotBeNull();
            property!.PropertyType.Should().Be(expectedType);
        }
    }
}