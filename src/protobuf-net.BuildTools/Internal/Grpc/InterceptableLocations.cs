#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>
    /// Obtains the <c>version</c>/<c>data</c> pair for an <c>[InterceptsLocation]</c>, from the host's
    /// Roslyn rather than from the version we compile against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>SemanticModel.GetInterceptableLocation()</c> is Roslyn <b>4.11+</b>, and this assembly compiles
    /// against 4.3.1 - which is deliberate, and is what lets <c>protobuf-net.BuildTools.Legacy</c> serve
    /// old SDKs. Reflection bridges that without moving the baseline, and the reason it is *sufficient*
    /// rather than a hack is a coincidence of versions: the <c>(version, data)</c> attribute form and this
    /// API arrived together in 4.11, so any host that would accept what we emit already has it. An older
    /// host has neither, and there is nothing to emit for it.
    /// </para>
    /// <para>
    /// The analyzer binds to the host's Roslyn at run time, which is the same reason
    /// <c>ProtoModelGenerator</c> spells <c>LanguageVersion.CSharp12</c> as a numeric constant. Reflection
    /// here is build-time only and carries none of AOT's constraints.
    /// </para>
    /// <para>
    /// The encoding is fully specified and was reproduced by hand (see <c>docs/aot-grpc.md</c>), so
    /// synthesising it ourselves is a proven fallback if this route ever fails - it needs an
    /// <c>xxHash128</c>, which Roslyn itself vendors. Calling the API is preferred because it tracks
    /// whatever encoding the compiler currently prefers, where synthesis pins us to version 1.
    /// </para>
    /// </remarks>
    internal static class InterceptableLocations
    {
        private static readonly MethodInfo? s_getLocation = FindGetInterceptableLocation();
        private static PropertyInfo? s_version, s_data;

        private static MethodInfo? FindGetInterceptableLocation()
        {
            // the extension lives on CSharpExtensions, which exists in every Roslyn we might bind to;
            // it is the *method* that may be absent
            var type = typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions);
            return (from method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    where method.Name == "GetInterceptableLocation"
                    let parameters = method.GetParameters()
                    where parameters.Length == 3
                        && parameters[0].ParameterType == typeof(SemanticModel)
                        && parameters[1].ParameterType == typeof(InvocationExpressionSyntax)
                        && parameters[2].ParameterType == typeof(CancellationToken)
                    select method).FirstOrDefault();
        }

        /// <summary>
        /// Whether the host's Roslyn can describe an interceptable location at all.
        /// </summary>
        public static bool IsSupported => s_getLocation is not null;

        /// <summary>
        /// The attribute arguments for a call site, or null if it is not interceptable - which the API
        /// reports for, among other things, a call whose type argument is not a concrete type.
        /// </summary>
        public static (int Version, string Data)? TryGet(SemanticModel model,
            InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            if (s_getLocation is null) return null;

            var location = s_getLocation.Invoke(null, new object[] { model, invocation, cancellationToken });
            if (location is null) return null;

            // resolved once, off the returned instance, so no type name is hard-coded anywhere
            s_version ??= location.GetType().GetProperty("Version");
            s_data ??= location.GetType().GetProperty("Data");
            if (s_version?.GetValue(location) is not int version) return null;
            if (s_data?.GetValue(location) is not string data) return null;

            return (version, data);
        }
    }
}
