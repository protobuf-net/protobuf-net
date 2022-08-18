using System;

namespace System.Diagnostics.CodeAnalysis;

#if NETSTANDARD2_0 || NETFRAMEWORK

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

#if NETSTANDARD || NETFRAMEWORK || NETCOREAPP3_1

internal sealed class MemberNotNullAttribute : Attribute
{
    public MemberNotNullAttribute(string member) => Members = new[] { member };

    public MemberNotNullAttribute(params string[] members) => Members = members;

    public string[] Members { get; }
}


internal sealed class MemberNotNullWhenAttribute : Attribute
{
    public MemberNotNullWhenAttribute(bool returnValue, string member)
    {
        ReturnValue = returnValue;
        Members = new[] { member };
    }

    public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
    {
        ReturnValue = returnValue;
        Members = members;
    }

    public bool ReturnValue { get; }

    public string[] Members { get; }
}

#endif