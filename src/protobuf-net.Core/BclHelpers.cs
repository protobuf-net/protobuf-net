using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    internal enum TimeSpanScale
    {
        Days = 0,
        Hours = 1,
        Minutes = 2,
        Seconds = 3,
        Milliseconds = 4,
        Ticks = 5,

        MinMax = 15
    }

    /// <summary>
    /// Provides support for common .NET types that do not have a direct representation
    /// in protobuf, using the definitions from bcl.proto
    /// </summary>
    public sealed class BclHelpers // should really be static, but I'm cheating with a <T>
    {
        private BclHelpers() { }
        /// <summary>
        /// Creates a new instance of the specified type, bypassing the constructor.
        /// </summary>
        /// <param name="type">The type to create</param>
        /// <returns>The new instance</returns>
        public static object GetUninitializedObject(Type type)
        {
            return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
        }

        private const int FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02, FieldTimeSpanKind = 0x03;

        internal static readonly DateTime[] EpochOrigin = {
            new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
            new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local)
        };

        /// <summary>
        /// The default value for dates that are following google.protobuf.Timestamp semantics
        /// </summary>
        private static readonly DateTime TimestampEpoch = EpochOrigin[(int)DateTimeKind.Utc];

        /// <summary>
        /// Writes a TimeSpan to a protobuf stream using protobuf-net's own representation, bcl.TimeSpan
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteTimeSpan(TimeSpan timeSpan, ProtoWriter dest)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteTimeSpanImpl(timeSpan, dest, DateTimeKind.Unspecified, ref state);
        }

        /// <summary>
        /// Writes a TimeSpan to a protobuf stream using protobuf-net's own representation, bcl.TimeSpan
        /// </summary>
        public static void WriteTimeSpan(TimeSpan timeSpan, ProtoWriter dest, ref ProtoWriter.State state)
        {
            WriteTimeSpanImpl(timeSpan, dest, DateTimeKind.Unspecified, ref state);
        }

        private static void WriteTimeSpanImpl(TimeSpan timeSpan, ProtoWriter dest, DateTimeKind kind, ref ProtoWriter.State state)
        {
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            long value;
            switch (dest.WireType)
            {
                case WireType.String:
                case WireType.StartGroup:
                    TimeSpanScale scale;
                    value = timeSpan.Ticks;
                    if (timeSpan == TimeSpan.MaxValue)
                    {
                        value = 1;
                        scale = TimeSpanScale.MinMax;
                    }
                    else if (timeSpan == TimeSpan.MinValue)
                    {
                        value = -1;
                        scale = TimeSpanScale.MinMax;
                    }
                    else if (value % TimeSpan.TicksPerDay == 0)
                    {
                        scale = TimeSpanScale.Days;
                        value /= TimeSpan.TicksPerDay;
                    }
                    else if (value % TimeSpan.TicksPerHour == 0)
                    {
                        scale = TimeSpanScale.Hours;
                        value /= TimeSpan.TicksPerHour;
                    }
                    else if (value % TimeSpan.TicksPerMinute == 0)
                    {
                        scale = TimeSpanScale.Minutes;
                        value /= TimeSpan.TicksPerMinute;
                    }
                    else if (value % TimeSpan.TicksPerSecond == 0)
                    {
                        scale = TimeSpanScale.Seconds;
                        value /= TimeSpan.TicksPerSecond;
                    }
                    else if (value % TimeSpan.TicksPerMillisecond == 0)
                    {
                        scale = TimeSpanScale.Milliseconds;
                        value /= TimeSpan.TicksPerMillisecond;
                    }
                    else
                    {
                        scale = TimeSpanScale.Ticks;
                    }

                    SubItemToken token = ProtoWriter.StartSubItem(null, dest, ref state);

                    if (value != 0)
                    {
                        ProtoWriter.WriteFieldHeader(FieldTimeSpanValue, WireType.SignedVariant, dest, ref state);
                        ProtoWriter.WriteInt64(value, dest, ref state);
                    }
                    if (scale != TimeSpanScale.Days)
                    {
                        ProtoWriter.WriteFieldHeader(FieldTimeSpanScale, WireType.Variant, dest, ref state);
                        ProtoWriter.WriteInt32((int)scale, dest, ref state);
                    }
                    if (kind != DateTimeKind.Unspecified)
                    {
                        ProtoWriter.WriteFieldHeader(FieldTimeSpanKind, WireType.Variant, dest, ref state);
                        ProtoWriter.WriteInt32((int)kind, dest, ref state);
                    }
                    ProtoWriter.EndSubItem(token, dest, ref state);
                    break;
                case WireType.Fixed64:
                    ProtoWriter.WriteInt64(timeSpan.Ticks, dest, ref state);
                    break;
                default:
                    throw new ProtoException("Unexpected wire-type: " + dest.WireType.ToString());
            }
        }

        /// <summary>
        /// Parses a TimeSpan from a protobuf stream using protobuf-net's own representation, bcl.TimeSpan
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static TimeSpan ReadTimeSpan(ProtoReader source)
        {
            ProtoReader.State state = source.DefaultState();
            return ReadTimeSpan(source, ref state);
        }

        /// <summary>
        /// Parses a TimeSpan from a protobuf stream using protobuf-net's own representation, bcl.TimeSpan
        /// </summary>        
        public static TimeSpan ReadTimeSpan(ProtoReader source, ref ProtoReader.State state)
        {
            long ticks = ReadTimeSpanTicks(source, ref state, out DateTimeKind kind);
            if (ticks == long.MinValue) return TimeSpan.MinValue;
            if (ticks == long.MaxValue) return TimeSpan.MaxValue;
            return TimeSpan.FromTicks(ticks);
        }

        /// <summary>
        /// Parses a TimeSpan from a protobuf stream using the standardized format, google.protobuf.Duration
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static TimeSpan ReadDuration(ProtoReader source)
        {
            ProtoReader.State state = source.DefaultState();
            return ReadDuration(source, ref state);
        }

        /// <summary>
        /// Parses a TimeSpan from a protobuf stream using the standardized format, google.protobuf.Duration
        /// </summary>
        public static TimeSpan ReadDuration(ProtoReader source, ref ProtoReader.State state)
        {
            if (source.WireType == WireType.String && state.RemainingInCurrent >= 20)
            {
                var ts = TryReadDurationFast(source, ref state);
                if (ts.HasValue) return ts.GetValueOrDefault();
            }
            return ReadDurationFallback(source, ref state);
        }

        private static TimeSpan? TryReadDurationFast(ProtoReader source, ref ProtoReader.State state)
        {
            int offset = state.OffsetInCurrent;
            var span = state.Span;
            int prefixLength = ProtoReader.State.ParseVarintUInt32(span, offset, out var len);
            offset += prefixLength;
            if (len == 0) return TimeSpan.Zero;

            if ((prefixLength + len) > state.RemainingInCurrent) return null; // don't have entire submessage

            if (span[offset] != (1 << 3)) return null; // expected field 1
            var msgOffset = 1 + ProtoReader.State.TryParseUInt64Varint(span, 1 + offset, out var seconds);
            ulong nanos = 0;
            if (msgOffset < len)
            {
                if (span[msgOffset++ + offset] != (2 << 3)) return null; // expected field 2
                msgOffset += ProtoReader.State.TryParseUInt64Varint(span, msgOffset + offset, out nanos);
            }
            if (msgOffset != len) return null; // expected no more fields
            state.Skip(prefixLength + (int)len);
            source.Advance(prefixLength + len);
            return FromDurationSeconds((long)seconds, (int)(long)nanos);
        }

        private static TimeSpan ReadDurationFallback(ProtoReader source, ref ProtoReader.State state)
        {
            long seconds = 0;
            int nanos = 0;
            SubItemToken token = ProtoReader.StartSubItem(source, ref state);
            int fieldNumber;
            while ((fieldNumber = source.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case 1:
                        seconds = source.ReadInt64(ref state);
                        break;
                    case 2:
                        nanos = source.ReadInt32(ref state);
                        break;
                    default:
                        source.SkipField(ref state);
                        break;
                }
            }
            ProtoReader.EndSubItem(token, source, ref state);
            return FromDurationSeconds(seconds, nanos);
        }

        /// <summary>
        /// Writes a TimeSpan to a protobuf stream using the standardized format, google.protobuf.Duration
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI)]
        public static void WriteDuration(TimeSpan value, ProtoWriter dest)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteDuration(value, dest, ref state);
        }

        /// <summary>
        /// Writes a TimeSpan to a protobuf stream using the standardized format, google.protobuf.Duration
        /// </summary>
        public static void WriteDuration(TimeSpan value, ProtoWriter dest, ref ProtoWriter.State state)
        {
            var seconds = ToDurationSeconds(value, out int nanos);
            WriteSecondsNanos(seconds, nanos, dest, ref state);
        }

        private static void WriteSecondsNanos(long seconds, int nanos, ProtoWriter dest, ref ProtoWriter.State state)
        {
            SubItemToken token = ProtoWriter.StartSubItem(null, dest, ref state);
            if (seconds != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, dest, ref state);
                ProtoWriter.WriteInt64(seconds, dest, ref state);
            }
            if (nanos != 0)
            {
                ProtoWriter.WriteFieldHeader(2, WireType.Variant, dest, ref state);
                ProtoWriter.WriteInt32(nanos, dest, ref state);
            }
            ProtoWriter.EndSubItem(token, dest, ref state);
        }

        /// <summary>
        /// Parses a DateTime from a protobuf stream using the standardized format, google.protobuf.Timestamp
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static DateTime ReadTimestamp(ProtoReader source)
        {
            ProtoReader.State state = source.DefaultState();
            return ReadTimestamp(source, ref state);
        }

        /// <summary>
        /// Parses a DateTime from a protobuf stream using the standardized format, google.protobuf.Timestamp
        /// </summary>
        public static DateTime ReadTimestamp(ProtoReader source, ref ProtoReader.State state)
        {
            // note: DateTime is only defined for just over 0000 to just below 10000;
            // TimeSpan has a range of +/- 10,675,199 days === 29k years;
            // so we can just use epoch time delta
            return TimestampEpoch + ReadDuration(source, ref state);
        }

        /// <summary>
        /// Writes a DateTime to a protobuf stream using the standardized format, google.protobuf.Timestamp
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteTimestamp(DateTime value, ProtoWriter dest)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteTimestamp(value, dest, ref state);
        }

        /// <summary>
        /// Writes a DateTime to a protobuf stream using the standardized format, google.protobuf.Timestamp
        /// </summary>
        public static void WriteTimestamp(DateTime value, ProtoWriter dest, ref ProtoWriter.State state)
        {
            var seconds = ToDurationSeconds(value - TimestampEpoch, out int nanos);

            if (nanos < 0)
            {   // from Timestamp.proto:
                // "Negative second values with fractions must still have
                // non -negative nanos values that count forward in time."
                seconds--;
                nanos += 1000000000;
            }
            WriteSecondsNanos(seconds, nanos, dest, ref state);
        }

        private static TimeSpan FromDurationSeconds(long seconds, int nanos)
        {
            long ticks = checked((seconds * TimeSpan.TicksPerSecond)
                + (nanos * TimeSpan.TicksPerMillisecond / 1000000));
            return TimeSpan.FromTicks(ticks);
        }

        private static long ToDurationSeconds(TimeSpan value, out int nanos)
        {
            nanos = (int)(((value.Ticks % TimeSpan.TicksPerSecond) * 1000000)
                / TimeSpan.TicksPerMillisecond);
            return value.Ticks / TimeSpan.TicksPerSecond;
        }

        /// <summary>
        /// Parses a DateTime from a protobuf stream
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static DateTime ReadDateTime(ProtoReader source)
        {
            ProtoReader.State state = source.DefaultState();
            return ReadDateTime(source, ref state);
        }

        /// <summary>
        /// Parses a DateTime from a protobuf stream
        /// </summary>
        public static DateTime ReadDateTime(ProtoReader source, ref ProtoReader.State state)
        {
            long ticks = ReadTimeSpanTicks(source, ref state, out DateTimeKind kind);
            if (ticks == long.MinValue) return DateTime.MinValue;
            if (ticks == long.MaxValue) return DateTime.MaxValue;
            return EpochOrigin[(int)kind].AddTicks(ticks);
        }

        /// <summary>
        /// Writes a DateTime to a protobuf stream, excluding the <c>Kind</c>
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteDateTime(DateTime value, ProtoWriter dest)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteDateTimeImpl(value, dest, false, ref state);
        }

        /// <summary>
        /// Writes a DateTime to a protobuf stream, excluding the <c>Kind</c>
        /// </summary>
        public static void WriteDateTime(DateTime value, ProtoWriter dest, ref ProtoWriter.State state)
        {
            WriteDateTimeImpl(value, dest, false, ref state);
        }

        /// <summary>
        /// Writes a DateTime to a protobuf stream, including the <c>Kind</c>
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteDateTimeWithKind(DateTime value, ProtoWriter dest)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteDateTimeImpl(value, dest, true, ref state);
        }

        /// <summary>
        /// Writes a DateTime to a protobuf stream, including the <c>Kind</c>
        /// </summary>
        public static void WriteDateTimeWithKind(DateTime value, ProtoWriter dest, ref ProtoWriter.State state)
        {
            WriteDateTimeImpl(value, dest, true, ref state);
        }

        private static void WriteDateTimeImpl(DateTime value, ProtoWriter dest, bool includeKind, ref ProtoWriter.State state)
        {
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            TimeSpan delta;
            switch (dest.WireType)
            {
                case WireType.StartGroup:
                case WireType.String:
                    if (value == DateTime.MaxValue)
                    {
                        delta = TimeSpan.MaxValue;
                        includeKind = false;
                    }
                    else if (value == DateTime.MinValue)
                    {
                        delta = TimeSpan.MinValue;
                        includeKind = false;
                    }
                    else
                    {
                        delta = value - EpochOrigin[0];
                    }
                    break;
                default:
                    delta = value - EpochOrigin[0];
                    break;
            }
            WriteTimeSpanImpl(delta, dest, includeKind ? value.Kind : DateTimeKind.Unspecified, ref state);
        }

        private static long ReadTimeSpanTicks(ProtoReader source, ref ProtoReader.State state, out DateTimeKind kind)
        {
            kind = DateTimeKind.Unspecified;
            switch (source.WireType)
            {
                case WireType.String:
                case WireType.StartGroup:
                    SubItemToken token = ProtoReader.StartSubItem(source, ref state);
                    int fieldNumber;
                    TimeSpanScale scale = TimeSpanScale.Days;
                    long value = 0;
                    while ((fieldNumber = source.ReadFieldHeader(ref state)) > 0)
                    {
                        switch (fieldNumber)
                        {
                            case FieldTimeSpanScale:
                                scale = (TimeSpanScale)source.ReadInt32(ref state);
                                break;
                            case FieldTimeSpanValue:
                                source.Assert(ref state, WireType.SignedVariant);
                                value = source.ReadInt64(ref state);
                                break;
                            case FieldTimeSpanKind:
                                kind = (DateTimeKind)source.ReadInt32(ref state);
                                switch (kind)
                                {
                                    case DateTimeKind.Unspecified:
                                    case DateTimeKind.Utc:
                                    case DateTimeKind.Local:
                                        break; // fine
                                    default:
                                        throw new ProtoException("Invalid date/time kind: " + kind.ToString());
                                }
                                break;
                            default:
                                source.SkipField(ref state);
                                break;
                        }
                    }
                    ProtoReader.EndSubItem(token, source, ref state);
                    switch (scale)
                    {
                        case TimeSpanScale.Days:
                            return value * TimeSpan.TicksPerDay;
                        case TimeSpanScale.Hours:
                            return value * TimeSpan.TicksPerHour;
                        case TimeSpanScale.Minutes:
                            return value * TimeSpan.TicksPerMinute;
                        case TimeSpanScale.Seconds:
                            return value * TimeSpan.TicksPerSecond;
                        case TimeSpanScale.Milliseconds:
                            return value * TimeSpan.TicksPerMillisecond;
                        case TimeSpanScale.Ticks:
                            return value;
                        case TimeSpanScale.MinMax:
                            switch (value)
                            {
                                case 1: return long.MaxValue;
                                case -1: return long.MinValue;
                                default: throw new ProtoException("Unknown min/max value: " + value.ToString());
                            }
                        default:
                            throw new ProtoException("Unknown timescale: " + scale.ToString());
                    }
                case WireType.Fixed64:
                    return source.ReadInt64(ref state);
                default:
                    throw new ProtoException("Unexpected wire-type: " + source.WireType.ToString());
            }
        }

        private const int FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;

        /// <summary>
        /// Parses a decimal from a protobuf stream
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static decimal ReadDecimal(ProtoReader reader)
        {
            ProtoReader.State state = reader.DefaultState();
            return ReadDecimal(reader, ref state);
        }
        /// <summary>
        /// Parses a decimal from a protobuf stream
        /// </summary>
        public static decimal ReadDecimal(ProtoReader reader, ref ProtoReader.State state)
        {
            ulong low = 0;
            uint high = 0;
            uint signScale = 0;

            int fieldNumber;
            SubItemToken token = ProtoReader.StartSubItem(reader, ref state);
            while ((fieldNumber = reader.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldDecimalLow: low = reader.ReadUInt64(ref state); break;
                    case FieldDecimalHigh: high = reader.ReadUInt32(ref state); break;
                    case FieldDecimalSignScale: signScale = reader.ReadUInt32(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            ProtoReader.EndSubItem(token, reader, ref state);

            int lo = (int)(low & 0xFFFFFFFFL),
                mid = (int)((low >> 32) & 0xFFFFFFFFL),
                hi = (int)high;
            bool isNeg = (signScale & 0x0001) == 0x0001;
            byte scale = (byte)((signScale & 0x01FE) >> 1);
            return new decimal(lo, mid, hi, isNeg, scale);
        }

        /// <summary>
        /// Writes a decimal to a protobuf stream
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteDecimal(decimal value, ProtoWriter writer)
        {
            ProtoWriter.State state = writer.DefaultState();
            WriteDecimal(value, writer, ref state);
        }

        private static
#if !DEBUG
        readonly
#endif
        bool s_decimalOptimized = VerifyDecimalLayout(),
            s_guidOptimized = VerifyGuidLayout();
        internal static bool DecimalOptimized
        {
            get => s_decimalOptimized;
#if DEBUG
            set => s_decimalOptimized = value && VerifyDecimalLayout();
#endif
        }
        internal static bool GuidOptimized
        {
            get => s_guidOptimized;
#if DEBUG
            set => s_guidOptimized = value && VerifyGuidLayout();
#endif
        }

        private static bool VerifyDecimalLayout()
        {
            try
            {
                // test against example taken from https://docs.microsoft.com/en-us/dotnet/api/system.decimal.getbits?view=netframework-4.8
                //     1.0000000000000000000000000000    001C0000  204FCE5E  3E250261  10000000
                var value = 1.0000000000000000000000000000M;
                var layout = new DecimalAccessor(value);
                if (layout.Lo == 0x10000000
                    & layout.Mid == 0x3E250261
                    & layout.Hi == 0x204FCE5E
                    & layout.Flags == 0x001C0000)
                {
                    // and double-check against GetBits itself
                    var bits = decimal.GetBits(value);
                    if (bits.Length == 4)
                    {
                        return layout.Lo == bits[0]
                            & layout.Mid == bits[1]
                            & layout.Hi == bits[2]
                            & layout.Flags == bits[3];
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool VerifyGuidLayout()
        {
            try
            {
                if (!Guid.TryParse("12345678-2345-3456-4567-56789a6789ab", out var guid))
                    return false;

                var obj = new GuidAccessor(guid);
                var low = obj.Low;
                var high = obj.High;

                // check it the fast way against our known sentinels
                if (low != 0x3456234512345678 | high != 0xAB89679A78566745) return false;

                // and do it "for real"
                var expected = guid.ToByteArray();
                for (int i = 0; i < 8; i++)
                {
                    if (expected[i] != (byte)(low >> (8 * i))) return false;
                }
                for (int i = 0; i < 8; i++)
                {
                    if (expected[i + 8] != (byte)(high >> (8 * i))) return false;
                }
                return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Writes a decimal to a protobuf stream
        /// </summary>
        public static void WriteDecimal(decimal value, ProtoWriter writer, ref ProtoWriter.State state)
        {
            ulong low;
            uint high, signScale;
            if (s_decimalOptimized) // the JIT should remove the non-preferred implementation, at least on modern runtimes
            {
                var dec = new DecimalAccessor(value);
                ulong a = ((ulong)dec.Mid) << 32, b = ((ulong)dec.Lo) & 0xFFFFFFFFL;
                low = a | b;
                high = (uint)dec.Hi;
                signScale = (uint)(((dec.Flags >> 15) & 0x01FE) | ((dec.Flags >> 31) & 0x0001));
            }
            else
            {
                int[] bits = decimal.GetBits(value);
                ulong a = ((ulong)bits[1]) << 32, b = ((ulong)bits[0]) & 0xFFFFFFFFL;
                low = a | b;
                high = (uint)bits[2];
                signScale = (uint)(((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));
            }
            SubItemToken token = ProtoWriter.StartSubItem(null, writer, ref state);

            if (low != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalLow, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt64(low, writer, ref state);
            }
            if (high != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalHigh, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt32(high, writer, ref state);
            }
            if (signScale != 0)
            {
                ProtoWriter.WriteFieldHeader(FieldDecimalSignScale, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt32(signScale, writer, ref state);
            }
            ProtoWriter.EndSubItem(token, writer, ref state);
        }

        private const int FieldGuidLow = 1, FieldGuidHigh = 2;

        /// <summary>
        /// Provides access to the inner fields of a decimal.
        /// Similar to decimal.GetBits(), but faster and avoids the int[] allocation
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct DecimalAccessor
        {
            [FieldOffset(0)]
            public readonly int Flags;
            [FieldOffset(4)]
            public readonly int Hi;
            [FieldOffset(8)]
            public readonly int Lo;
            [FieldOffset(12)]
            public readonly int Mid;

            [FieldOffset(0)]
            public readonly decimal Decimal;

            public DecimalAccessor(decimal value)
            {
                this = default;
                Decimal = value;
            }
        }

        /// <summary>
        /// Provides access to the inner fields of a Guid.
        /// Similar to Guid.ToByteArray(), but faster and avoids the byte[] allocation
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct GuidAccessor
        {
            [FieldOffset(0)]
            public readonly Guid Guid;

            [FieldOffset(0)]
            public readonly ulong Low;

            [FieldOffset(8)]
            public readonly ulong High;

            public GuidAccessor(Guid value)
            {
                Low = High = default;
                Guid = value;
            }

            public GuidAccessor(ulong low, ulong high)
            {
                Guid = default;
                Low = low;
                High = high;
            }
        }

        /// <summary>
        /// Writes a Guid to a protobuf stream
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteGuid(Guid value, ProtoWriter dest)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteGuid(value, dest, ref state);
        }

        /// <summary>
        /// Writes a Guid to a protobuf stream
        /// </summary>        
        public static void WriteGuid(Guid value, ProtoWriter dest, ref ProtoWriter.State state)
        {
            SubItemToken token = ProtoWriter.StartSubItem(null, dest, ref state);
            if (value != Guid.Empty)
            {
                if (s_guidOptimized)
                {
                    var obj = new GuidAccessor(value);
                    ProtoWriter.WriteFieldHeader(FieldGuidLow, WireType.Fixed64, dest, ref state);
                    ProtoWriter.WriteUInt64(obj.Low, dest, ref state);
                    ProtoWriter.WriteFieldHeader(FieldGuidHigh, WireType.Fixed64, dest, ref state);
                    ProtoWriter.WriteUInt64(obj.High, dest, ref state);
                }
                else
                {
                    byte[] blob = value.ToByteArray();
                    ProtoWriter.WriteFieldHeader(FieldGuidLow, WireType.Fixed64, dest, ref state);
                    ProtoWriter.WriteBytes(blob, 0, 8, dest, ref state);
                    ProtoWriter.WriteFieldHeader(FieldGuidHigh, WireType.Fixed64, dest, ref state);
                    ProtoWriter.WriteBytes(blob, 8, 8, dest, ref state);
                }
            }
            ProtoWriter.EndSubItem(token, dest, ref state);
        }

        /// <summary>
        /// Parses a Guid from a protobuf stream
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static Guid ReadGuid(ProtoReader source)
        {
            ProtoReader.State state = source.DefaultState();
            return ReadGuid(source, ref state);
        }

        /// <summary>
        /// Parses a Guid from a protobuf stream
        /// </summary>
        public static Guid ReadGuid(ProtoReader source, ref ProtoReader.State state)
        {
            ulong low = 0, high = 0;
            int fieldNumber;
            SubItemToken token = ProtoReader.StartSubItem(source, ref state);
            while ((fieldNumber = source.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldGuidLow: low = source.ReadUInt64(ref state); break;
                    case FieldGuidHigh: high = source.ReadUInt64(ref state); break;
                    default: source.SkipField(ref state); break;
                }
            }
            ProtoReader.EndSubItem(token, source, ref state);
            if (low == 0 && high == 0) return Guid.Empty;
            if (s_guidOptimized)
            {
                var obj = new GuidAccessor(low, high);
                return obj.Guid;
            }
            else
            {
                uint a = (uint)(low >> 32), b = (uint)low, c = (uint)(high >> 32), d = (uint)high;
                return new Guid((int)b, (short)a, (short)(a >> 16),
                    (byte)d, (byte)(d >> 8), (byte)(d >> 16), (byte)(d >> 24),
                    (byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24));
            }
        }

        private const int
            FieldExistingObjectKey = 1,
            FieldNewObjectKey = 2,
            FieldExistingTypeKey = 3,
            FieldNewTypeKey = 4,
            FieldTypeName = 8,
            FieldObject = 10;

        /// <summary>
        /// Optional behaviours that introduce .NET-specific functionality
        /// </summary>
        [Flags]
        public enum NetObjectOptions : byte
        {
            /// <summary>
            /// No special behaviour
            /// </summary>
            None = 0,
            /// <summary>
            /// Enables full object-tracking/full-graph support.
            /// </summary>
            AsReference = 1,
            /// <summary>
            /// Embeds the type information into the stream, allowing usage with types not known in advance.
            /// </summary>
            DynamicType = 2,
            /// <summary>
            /// If false, the constructor for the type is bypassed during deserialization, meaning any field initializers
            /// or other initialization code is skipped.
            /// </summary>
            UseConstructor = 4,
            /// <summary>
            /// Should the object index be reserved, rather than creating an object promptly
            /// </summary>
            LateSet = 8
        }

        /// <summary>
        /// Reads an *implementation specific* bundled .NET object, including (as options) type-metadata, identity/re-use, etc.
        /// </summary>
        [Obsolete(ProtoReader.UseStateAPI, false)]
        public static object ReadNetObject(object value, ProtoReader source, int key, Type type, NetObjectOptions options)
        {
            ProtoReader.State state = source.DefaultState();
            return ReadNetObject(source, ref state, value, key, type, options);
        }

        /// <summary>
        /// Reads an *implementation specific* bundled .NET object, including (as options) type-metadata, identity/re-use, etc.
        /// </summary>
        public static object ReadNetObject(ProtoReader source, ref ProtoReader.State state, object value, int key, Type type, NetObjectOptions options)
        {
            SubItemToken token = ProtoReader.StartSubItem(source, ref state);
            int fieldNumber;
            int newObjectKey = -1, newTypeKey = -1, tmp;
            while ((fieldNumber = source.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case FieldExistingObjectKey:
                        tmp = source.ReadInt32(ref state);
                        value = source.NetCache.GetKeyedObject(tmp);
                        break;
                    case FieldNewObjectKey:
                        newObjectKey = source.ReadInt32(ref state);
                        break;
                    case FieldExistingTypeKey:
                        tmp = source.ReadInt32(ref state);
                        type = (Type)source.NetCache.GetKeyedObject(tmp);
                        key = source.GetTypeKey(ref type);
                        break;
                    case FieldNewTypeKey:
                        newTypeKey = source.ReadInt32(ref state);
                        break;
                    case FieldTypeName:
                        string typeName = source.ReadString(ref state);
                        type = source.DeserializeType(typeName);
                        if (type == null)
                        {
                            throw new ProtoException("Unable to resolve type: " + typeName + " (you can use the TypeModel.DynamicTypeFormatting event to provide a custom mapping)");
                        }
                        if (type == typeof(string))
                        {
                            key = -1;
                        }
                        else
                        {
                            key = source.GetTypeKey(ref type);
                            if (key < 0)
                                throw new InvalidOperationException("Dynamic type is not a contract-type: " + type.Name);
                        }
                        break;
                    case FieldObject:
                        bool isString = type == typeof(string);
                        bool wasNull = value == null;
                        bool lateSet = wasNull && (isString || ((options & NetObjectOptions.LateSet) != 0));

                        if (newObjectKey >= 0 && !lateSet)
                        {
                            if (value == null)
                            {
                                source.TrapNextObject(newObjectKey);
                            }
                            else
                            {
                                source.NetCache.SetKeyedObject(newObjectKey, value);
                            }
                            if (newTypeKey >= 0) source.NetCache.SetKeyedObject(newTypeKey, type);
                        }
                        object oldValue = value;
                        if (isString)
                        {
                            value = source.ReadString(ref state);
                        }
                        else
                        {
                            value = ProtoReader.ReadTypedObject(source, ref state, oldValue, key, type);
                        }

                        if (newObjectKey >= 0)
                        {
                            if (wasNull && !lateSet)
                            { // this both ensures (via exception) that it *was* set, and makes sure we don't shout
                                // about changed references
                                oldValue = source.NetCache.GetKeyedObject(newObjectKey);
                            }
                            if (lateSet)
                            {
                                source.NetCache.SetKeyedObject(newObjectKey, value);
                                if (newTypeKey >= 0) source.NetCache.SetKeyedObject(newTypeKey, type);
                            }
                        }
                        if (newObjectKey >= 0 && !lateSet && !ReferenceEquals(oldValue, value))
                        {
                            throw new ProtoException("A reference-tracked object changed reference during deserialization");
                        }
                        if (newObjectKey < 0 && newTypeKey >= 0)
                        {  // have a new type, but not a new object
                            source.NetCache.SetKeyedObject(newTypeKey, type);
                        }
                        break;
                    default:
                        source.SkipField(ref state);
                        break;
                }
            }
            if (newObjectKey >= 0 && (options & NetObjectOptions.AsReference) == 0)
            {
                throw new ProtoException("Object key in input stream, but reference-tracking was not expected");
            }
            ProtoReader.EndSubItem(token, source, ref state);

            return value;
        }

        /// <summary>
        /// Writes an *implementation specific* bundled .NET object, including (as options) type-metadata, identity/re-use, etc.
        /// </summary>
        [Obsolete(ProtoWriter.UseStateAPI, false)]
        public static void WriteNetObject(object value, ProtoWriter dest, int key, NetObjectOptions options)
        {
            ProtoWriter.State state = dest.DefaultState();
            WriteNetObject(value, dest, ref state, key, options);
        }

        /// <summary>
        /// Writes an *implementation specific* bundled .NET object, including (as options) type-metadata, identity/re-use, etc.
        /// </summary>
        public static void WriteNetObject(object value, ProtoWriter dest, ref ProtoWriter.State state, int key, NetObjectOptions options)
        {
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            bool dynamicType = (options & NetObjectOptions.DynamicType) != 0,
                 asReference = (options & NetObjectOptions.AsReference) != 0;
            WireType wireType = dest.WireType;
            SubItemToken token = ProtoWriter.StartSubItem(null, dest, ref state);
            bool writeObject = true;
            if (asReference)
            {
                int objectKey = dest.NetCache.AddObjectKey(value, out bool existing);
                ProtoWriter.WriteFieldHeader(existing ? FieldExistingObjectKey : FieldNewObjectKey, WireType.Variant, dest, ref state);
                ProtoWriter.WriteInt32(objectKey, dest, ref state);
                if (existing)
                {
                    writeObject = false;
                }
            }

            if (writeObject)
            {
                if (dynamicType)
                {
                    Type type = value.GetType();

                    if (!(value is string))
                    {
                        key = dest.GetTypeKey(ref type);
                        if (key < 0) throw new InvalidOperationException("Dynamic type is not a contract-type: " + type.Name);
                    }
                    int typeKey = dest.NetCache.AddObjectKey(type, out bool existing);
                    ProtoWriter.WriteFieldHeader(existing ? FieldExistingTypeKey : FieldNewTypeKey, WireType.Variant, dest, ref state);
                    ProtoWriter.WriteInt32(typeKey, dest, ref state);
                    if (!existing)
                    {
                        ProtoWriter.WriteFieldHeader(FieldTypeName, WireType.String, dest, ref state);
                        ProtoWriter.WriteString(dest.SerializeType(type), dest, ref state);
                    }
                }
                ProtoWriter.WriteFieldHeader(FieldObject, wireType, dest, ref state);
                if (value is string s)
                {
                    ProtoWriter.WriteString(s, dest, ref state);
                }
                else
                {
                    ProtoWriter.WriteObject(value, key, dest, ref state);
                }
            }
            ProtoWriter.EndSubItem(token, dest, ref state);
        }
    }
}