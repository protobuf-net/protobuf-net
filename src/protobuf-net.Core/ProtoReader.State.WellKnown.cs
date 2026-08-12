using ProtoBuf.Internal;
using ProtoBuf.WellKnownTypes;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    partial class ProtoReader
    {
        public ref partial struct State
        {
            // ---- self-framing raw readers for the compatibility-level BCL types ----
            // The tag was consumed by the caller (raw convention); these frame the
            // length-prefixed body themselves and delegate to the SAME loop bodies the
            // stateful serializers now run (PrimaryTypeProvider - "change BclHelpers to use
            // the raw API"), so there is exactly one decode per shape. Fixed64/group
            // framings of these types stay on the stateful path via StashTag arms in the
            // generated code, so these only ever see wire 2.

            /// <summary>Raw-convention read of a bcl.TimeSpan (compatibility level 200).</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public TimeSpan ReadRawTimeSpanBcl()
            {
                var scope = PushLengthPrefix();
                var ticks = PrimaryTypeProvider.ReadRawScaledTicksBody(ref this);
                PopScope(scope);
                return ticks.ToTimeSpan();
            }

            /// <summary>Raw-convention read of a bcl.DateTime (compatibility level 200).</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public DateTime ReadRawDateTimeBcl()
            {
                var scope = PushLengthPrefix();
                var ticks = PrimaryTypeProvider.ReadRawScaledTicksBody(ref this);
                PopScope(scope);
                return ticks.ToDateTime();
            }

            /// <summary>Raw-convention read of a google.protobuf.Duration (level 240+).</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public TimeSpan ReadRawDuration()
            {
                long seconds = 0;
                int nanos = 0;
                if (TryReadWellKnownPairFast(ref seconds, ref nanos))
                {
                    return new Duration(seconds, nanos).AsTimeSpan();
                }
                var scope = PushLengthPrefix();
                var duration = PrimaryTypeProvider.ReadRawSecondsNanosBody(ref this, default);
                PopScope(scope);
                return duration.AsTimeSpan();
            }

            /// <summary>Raw-convention read of a google.protobuf.Timestamp (level 240+).</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public DateTime ReadRawTimestamp()
            {
                long seconds = 0;
                int nanos = 0;
                if (!TryReadWellKnownPairFast(ref seconds, ref nanos))
                {
                    var scope = PushLengthPrefix();
                    var duration = PrimaryTypeProvider.ReadRawSecondsNanosBody(ref this, default);
                    PopScope(scope);
                    seconds = duration.Seconds;
                    nanos = duration.Nanoseconds;
                }
                return new Timestamp(seconds, nanos).AsDateTime();
            }

            /// <summary>Raw-convention read of a bcl.Guid (compatibility levels 200/240).</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public Guid ReadRawGuidBcl()
            {
                var scope = PushLengthPrefix();
                var guid = PrimaryTypeProvider.ReadRawGuidBody(ref this);
                PopScope(scope);
                return guid;
            }

            /// <summary>Raw-convention read of a bcl.Decimal (compatibility levels 200/240).</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public decimal ReadRawDecimalBcl()
            {
                var scope = PushLengthPrefix();
                var result = PrimaryTypeProvider.ReadRawDecimalBody(ref this);
                PopScope(scope);
                return result;
            }

            /// <summary>
            /// The Duration/Timestamp fast path: a fully-resident peek of the canonical
            /// [len][field 1 varint][field 2 varint] sub-message shape, committing only on a
            /// complete match - forward-only is honored because a bail consumes nothing and a
            /// match commits exactly once. The caller has already established WireType.String.
            /// </summary>
            internal bool TryReadWellKnownPairFast(ref long seconds, ref int nanos)
            {
                // conservative residency, as the legacy fast path: prefix + two 10-byte varints
                if (_count - _offset < 20) return false;
                int cursor = _offset;
                if (!TryPeekVarint(ref cursor, out ulong len64)) return false;
                if (len64 == 0)
                {
                    _offset = cursor; // empty message: both fields default
                    return true;
                }
                if (len64 > int.MaxValue) return false;
                int len = (int)len64;
                if ((cursor - _offset) + len > _count - _offset) return false; // not fully resident
                int end = cursor + len;

                if (At(cursor) != (1 << 3)) return false; // expected field 1, varint
                cursor++;
                if (!TryPeekVarint(ref cursor, out ulong secs)) return false;
                long newNanos = nanos;
                if (cursor < end)
                {
                    if (At(cursor) != (2 << 3)) return false; // expected field 2, varint
                    cursor++;
                    if (!TryPeekVarint(ref cursor, out ulong tmp)) return false;
                    newNanos = (long)tmp;
                }
                if (cursor != end) return false; // expected no more fields

                _offset = end; // the single commit
                seconds = (long)secs;
                nanos = (int)newNanos;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly bool TryPeekVarint(ref int cursor, out ulong value)
            {
                value = 0;
                int shift = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (cursor >= _count) return false;
                    ulong b = At(cursor++);
                    value |= (b & 0x7F) << shift;
                    if ((b & 0x80) == 0) return true;
                    shift += 7;
                }
                return false;
            }
        }
    }
}
