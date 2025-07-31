using System.ComponentModel;

#if !NET5_0_OR_GREATER

// ReSharper disable CheckNamespace # needs to be well-known

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class IsExternalInit{}
}

#endif