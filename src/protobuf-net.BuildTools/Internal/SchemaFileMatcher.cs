#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.BuildTools.Internal
{
    /// <summary>
    /// Resolves the schema named by a seed attribute (<c>docs/aot-schema-model.md</c>) against the
    /// compilation's additional files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The requirement that shapes this: <c>"foo/bar/blap.proto"</c> and <c>"x/blap.proto"</c> are
    /// DIFFERENT files and must stay distinguishable, while <c>/</c> and <c>\</c> are the same
    /// separator and must not be. So matching is on whole path SEGMENTS from the right, never on
    /// raw substrings - <c>"bar/blap.proto"</c> matches <c>foo/bar/blap.proto</c>, and
    /// <c>"ar/blap.proto"</c> matches nothing.
    /// </para>
    /// <para>
    /// A bare leaf name is allowed and is the common case, but only while it is unambiguous:
    /// two additional files with the same leaf make <c>"blap.proto"</c> an ERROR naming both,
    /// rather than a silent pick. That is the whole reason this is a resolver and not a
    /// <c>string.EndsWith</c>.
    /// </para>
    /// </remarks>
    internal static class SchemaFileMatcher
    {
        internal enum MatchResult
        {
            Matched,
            NotFound,
            Ambiguous,
        }

        /// <summary>
        /// Finds the single additional file the request names.
        /// </summary>
        /// <param name="requested">The path as written in the attribute; may use either separator.</param>
        /// <param name="candidates">The additional-file paths, as the compilation reports them.</param>
        /// <param name="match">The matching candidate, exactly as it was supplied.</param>
        /// <param name="detail">On failure, the candidates that made it a failure (may be empty).</param>
        internal static MatchResult TryMatch(string requested, IReadOnlyList<string> candidates,
            out string? match, out IReadOnlyList<string> detail)
        {
            match = null;
            detail = Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(requested) || candidates is null || candidates.Count == 0)
            {
                return MatchResult.NotFound;
            }

            var want = Normalize(requested);

            // an exact whole-path match wins outright, so a consumer who writes the full path is
            // never told it is ambiguous with something it is a suffix of
            List<string>? exact = null;
            foreach (var candidate in candidates)
            {
                if (string.Equals(Normalize(candidate), want, StringComparison.OrdinalIgnoreCase))
                {
                    (exact ??= new List<string>()).Add(candidate);
                }
            }
            if (exact is { Count: 1 })
            {
                match = exact[0];
                return MatchResult.Matched;
            }
            if (exact is { Count: > 1 })
            {
                detail = exact;
                return MatchResult.Ambiguous;
            }

            // otherwise a suffix, on segment boundaries only
            List<string>? suffix = null;
            foreach (var candidate in candidates)
            {
                if (IsSegmentSuffix(Normalize(candidate), want))
                {
                    (suffix ??= new List<string>()).Add(candidate);
                }
            }
            switch (suffix?.Count ?? 0)
            {
                case 0:
                    return MatchResult.NotFound;
                case 1:
                    match = suffix![0];
                    return MatchResult.Matched;
                default:
                    detail = suffix!;
                    return MatchResult.Ambiguous;
            }
        }

        /// <summary>
        /// Both separators become <c>/</c>; a leading <c>./</c> is dropped; trailing separators go.
        /// Case is NOT folded here - the comparisons do that, so the original casing survives for
        /// diagnostics.
        /// </summary>
        private static string Normalize(string path)
        {
            var sb = new StringBuilder(path.Length);
            foreach (var c in path) sb.Append(c == '\\' ? '/' : c);
            var s = sb.ToString();
            while (s.StartsWith("./", StringComparison.Ordinal)) s = s.Substring(2);
            return s.TrimEnd('/');
        }

        /// <summary>
        /// True when <paramref name="want"/> is a whole-segment tail of <paramref name="candidate"/>
        /// - so <c>bar/blap.proto</c> matches <c>foo/bar/blap.proto</c> but <c>ar/blap.proto</c>
        /// does not.
        /// </summary>
        private static bool IsSegmentSuffix(string candidate, string want)
        {
            if (candidate.Length < want.Length) return false;
            if (!candidate.EndsWith(want, StringComparison.OrdinalIgnoreCase)) return false;
            if (candidate.Length == want.Length) return true;
            return candidate[candidate.Length - want.Length - 1] == '/';
        }
    }
}
