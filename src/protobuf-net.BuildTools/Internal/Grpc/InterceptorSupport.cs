#nullable enable
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>
    /// Whether C# interceptors are switched on for the namespace we would emit into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This has to be asked before emitting anything, and the reason is that the failure is an
    /// <em>error</em>: an <c>[InterceptsLocation]</c> in a namespace the consumer has not opted into is
    /// <c>CS9137</c>, so emitting optimistically would break the build of a project that merely had not
    /// asked for the feature. Verified by doing it, not inferred.
    /// </para>
    /// <para>
    /// The consumer opts in with
    /// <c>&lt;InterceptorsNamespaces&gt;$(InterceptorsNamespaces);ProtoBuf.AOT&lt;/InterceptorsNamespaces&gt;</c>.
    /// Both that and the older <c>InterceptorsPreviewNamespaces</c> spelling are honoured - the compiler's
    /// own CS9137 text names the non-preview one today, but the preview form is what is written down in a
    /// lot of places (DapperAOT's getting-started among them) and accepting either costs nothing.
    /// </para>
    /// </remarks>
    internal static class InterceptorSupport
    {
        /// <summary>The namespace the generated interceptors live in; see <c>notes/aot-grpc.md</c>.</summary>
        public const string Namespace = "ProtoBuf.AOT";

        private const string FeatureKey = "InterceptorsNamespaces";
        private const string PreviewFeatureKey = "InterceptorsPreviewNamespaces";

        /// <summary>
        /// Whether an interceptor declared in <see cref="Namespace"/> would be accepted.
        /// </summary>
        public static bool IsEnabled(CSharpParseOptions? options)
            => IsEnabled(options, Namespace);

        internal static bool IsEnabled(CSharpParseOptions? options, string targetNamespace)
        {
            if (options is null) return false;

            foreach (var pair in options.Features)
            {
                // The keys arrive from /features:, which the Csc task populates from the two MSBuild
                // properties of the same names. Matched case-insensitively rather than pinning a casing:
                // the value we need is the consumer's namespace list, and being wrong about the key's
                // capitalisation would silently disable the whole feature.
                if (!string.Equals(pair.Key, FeatureKey, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(pair.Key, PreviewFeatureKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (Covers(pair.Value, targetNamespace)) return true;
            }
            return false;
        }

        /// <summary>
        /// Whether a semicolon-separated namespace list enables <paramref name="targetNamespace"/>.
        /// </summary>
        /// <remarks>
        /// Enablement is by <b>prefix</b>, on namespace boundaries - probed rather than assumed, by
        /// declaring an interceptor in <c>ProtoBuf.AOT.Grpc</c> while enabling only <c>ProtoBuf.AOT</c>
        /// and watching for CS9234 ("location not found", i.e. the namespace *was* enabled) rather than
        /// CS9137. So listing <c>ProtoBuf</c> or <c>ProtoBuf.AOT</c> both cover us, and that is why the
        /// namespace can be a single permanent entry in the consumer's project file.
        /// </remarks>
        internal static bool Covers(string? namespaceList, string targetNamespace)
        {
            if (string.IsNullOrEmpty(namespaceList)) return false;

            foreach (var candidate in namespaceList!.Split(';'))
            {
                var trimmed = candidate.Trim();
                if (trimmed.Length == 0) continue;

                if (string.Equals(trimmed, targetNamespace, StringComparison.Ordinal)) return true;

                // a prefix only counts on a namespace boundary: "ProtoBuf.AO" must not match
                // "ProtoBuf.AOT", where "ProtoBuf" must
                if (targetNamespace.Length > trimmed.Length
                    && targetNamespace[trimmed.Length] == '.'
                    && targetNamespace.StartsWith(trimmed, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The line a consumer has to add, for use in diagnostics and documentation.
        /// </summary>
        public static string OptInSnippet
            => $"<{FeatureKey}>$({FeatureKey});{Namespace}</{FeatureKey}>";
    }
}
