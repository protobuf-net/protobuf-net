#if NETSTANDARD2_0 || NETFRAMEWORK

namespace System.Diagnostics.CodeAnalysis;

internal sealed class NotNullAttribute : Attribute { }

internal sealed class DoesNotReturnAttribute : Attribute { }

internal sealed class NotNullIfNotNullAttribute : Attribute
{
    public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;

    public string ParameterName { get; }
}

internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    public bool ReturnValue { get; }
}

#endif