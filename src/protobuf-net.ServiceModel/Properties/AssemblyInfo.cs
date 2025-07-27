
using System.Runtime.CompilerServices;

#if !NETSTANDARD2_0_OR_GREATER // see #1214
[module: SkipLocalsInit]
#endif