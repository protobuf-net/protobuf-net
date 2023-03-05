using System;
using System.Text;

namespace ProtoBuf.Extensions
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder NewLine(this StringBuilder builder, ref int pos, int indent) 
            => builder.Insert(Environment.NewLine, ref pos)
                      .Insert(" ", ref pos, indent * 3);

        public static StringBuilder Insert(this StringBuilder builder, string value, ref int pos, Func<bool> condition, int count = 1)
        {
            if (!condition()) return builder;
            return builder.Insert(value, ref pos, count);
        }

        public static StringBuilder Insert(this StringBuilder builder, string value, ref int pos, int count = 1)
        {
            var previousLength = builder.Length;
            builder.Insert(pos, value, count);
            var builderLengthDiff = builder.Length - previousLength;
            pos += builderLengthDiff;
            return builder;
        }
    }
}
