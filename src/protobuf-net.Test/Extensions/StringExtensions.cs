using System.Text.RegularExpressions;

namespace ProtoBuf.Test.Extensions
{
    internal static class StringExtensions
    {
        public static string RemoveEmptyLines(this string str) 
            => Regex.Replace(str, @"^\r?\n?$", "", RegexOptions.Multiline);
        
        public static string RemoveWhitespacesInLineStart(this string str) 
            => Regex.Replace(str, @"(?<=\n)\s+", "");
    }
}