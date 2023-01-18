using System;
using System.Globalization;

namespace ProtoBuf.Reflection.Internal
{
    /// <summary>Attempt to avoid boxes for common values</summary>
    internal static class BoxFunctions
    {
        public static readonly Func<string, object> String = value => value;
        public static readonly Func<byte[], object> ByteArray = value => value;
        public static readonly Func<object, object> Object = value => value;

        private static readonly object BooleanTrue = true, BooleanFalse = false;
        public static readonly Func<bool, object> Boolean = value => value ? BooleanTrue : BooleanFalse;

        private static readonly object[] Int32Low = new object[] { (int)-1, (int)0, (int)1, (int)2, (int)3, (int)4, (int)5, (int)6, (int)7, (int)8, (int)9, (int)10 };
        public static readonly Func<int, object> Int32 = value => (value >= -1 && value <= 10) ? Int32Low[value + 1] : value;

        private static readonly string[] Int32LowString = new[] { "-1", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
        internal static string Int32String(int value, IFormatProvider formatProvider) => (value >= -1 && value <= 10 && ReferenceEquals(formatProvider, CultureInfo.InvariantCulture))
            ? Int32LowString[value + 1] : value.ToString(formatProvider);

        static readonly object[] Int64Low = new object[] { (long)-1, (long)0, (long)1, (long)2, (long)3, (long)4, (long)5, (long)6, (long)7, (long)8, (long)9, (long)10 };
        public static readonly Func<long, object> Int64 = value => (value >= -1 && value <= 10) ? Int64Low[value + 1] : value;

        static readonly object[] UInt32Low = new object[] { (uint)0, (uint)1, (uint)2, (uint)3, (uint)4, (uint)5, (uint)6, (uint)7, (uint)8, (uint)9, (uint)10 };
        public static readonly Func<uint, object> UInt32 = value => value <= 10 ? UInt32Low[value] : value;

        private static readonly object[] UInt64Low = new object[] { (ulong)0, (ulong)1, (ulong)2, (ulong)3, (ulong)4, (ulong)5, (ulong)6, (ulong)7, (ulong)8, (ulong)9, (ulong)10 };
        public static readonly Func<ulong, object> UInt64 = value => value <= 10 ? UInt64Low[value] : value;

        private static readonly object SingleZero = 0F, SingleOne = 1F, SingleMinusOne = -1F, SinglePositiveInfinity = float.PositiveInfinity, SingleNegativeInfinity = float.NegativeInfinity, SingleNaN = double.NaN;
        public static readonly Func<float, object> Single = value =>
        {
            if (value == 0) return SingleZero;
            if (value == 1) return SingleOne;
            if (value == -1) return SingleMinusOne;
            if (float.IsInfinity(value)) return float.IsPositiveInfinity(value) ? SinglePositiveInfinity : SingleNegativeInfinity;
            if (float.IsNaN(value)) return SingleNaN;
            return value;
        };

        private static readonly object DoubleZero = 0D, DoubleOne = 1D, DoubleMinusOne = -1D, DoublePositiveInfinity = double.PositiveInfinity, DoubleNegativeInfinity = double.NegativeInfinity, DoubleNaN = double.NaN;
        public static readonly Func<double, object> Double = value =>
        {
            if (value == 0) return DoubleZero;
            if (value == 1) return DoubleOne;
            if (value == -1) return DoubleMinusOne;
            if (double.IsInfinity(value)) return double.IsPositiveInfinity(value) ? DoublePositiveInfinity : DoubleNegativeInfinity;
            if (double.IsNaN(value)) return DoubleNaN;
            return value;
        };
    }
}
