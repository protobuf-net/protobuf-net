#nullable enable
using System;

namespace ProtoBuf.BuildTools.Internal
{
    internal interface ILoggingAnalyzer
    {
        event Action<string>? Log;
    }
}
