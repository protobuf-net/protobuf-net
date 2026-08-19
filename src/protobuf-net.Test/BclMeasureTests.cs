using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// The <c>BclHelpers.Measure*</c> family must equal what the matching <c>Write*</c> produces —
    /// `notes/gaps.md` B26.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These exist so a compile-time serializer can size a <c>DateTime</c>/<c>TimeSpan</c> member
    /// arithmetically instead of writing it twice. The measure and the writer are two expressions
    /// of one format, and if they disagree the length prefix is wrong and the payload is corrupt —
    /// so this compares against <b>bytes actually written by protobuf-net</b>, not against a second
    /// copy of the arithmetic.
    /// </para>
    /// <para>
    /// The values are chosen to move the parts the format actually varies on: the scale (a
    /// <c>TimeSpan</c> that is a whole number of days encodes differently from one with ticks), the
    /// sign (field 1 is zigzag, so negatives matter), and zero (which omits the field entirely,
    /// making the body empty).
    /// </para>
    /// </remarks>
    public class BclMeasureTests
    {
        [ProtoContract]
        public class HasTimeSpan { [ProtoMember(1)] public TimeSpan Value { get; set; } }

        [ProtoContract]
        public class HasDateTime { [ProtoMember(1)] public DateTime Value { get; set; } }

        public static IEnumerable<object[]> TimeSpans()
        {
            foreach (var v in new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromDays(1),           // a whole day: a different scale from ticks
                TimeSpan.FromDays(-1),
                TimeSpan.FromHours(3),
                TimeSpan.FromMinutes(-90),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromTicks(1),
                TimeSpan.FromTicks(-1),
                TimeSpan.MaxValue,
                TimeSpan.MinValue,
            }) yield return [v];
        }

        public static IEnumerable<object[]> DateTimes()
        {
            foreach (var v in new[]
            {
                default(DateTime),
                new DateTime(1970, 1, 1),       // the epoch: the zero point, so the body is empty
                new DateTime(2026, 8, 16),
                new DateTime(1900, 1, 1),       // before the epoch: negative, hence zigzag
                new DateTime(2026, 8, 16, 13, 45, 30, 123),
                DateTime.MaxValue,
                DateTime.MinValue,
            }) yield return [v];
        }

        [ProtoContract]
        public class HasGuid { [ProtoMember(1)] public Guid Value { get; set; } }

        [ProtoContract]
        public class HasDecimal { [ProtoMember(1)] public decimal Value { get; set; } }

        public static IEnumerable<object[]> Guids()
        {
            foreach (var v in new[]
            {
                Guid.Empty,
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                Guid.Parse("5bad8f0f-cbd9-9f46-a165-708677289505"),
            }) yield return [v];
        }

        public static IEnumerable<object[]> Decimals()
        {
            foreach (var v in new[]
            {
                0m, 1m, -1m,
                0.0001m,                 // scale in play, so the signScale field is non-zero
                -0.0001m,
                decimal.MaxValue,
                decimal.MinValue,
                79228162514264337593543950335m,
                123456789.987654321m,
            }) yield return [v];
        }

        [Theory, MemberData(nameof(Guids))]
        public void MeasureGuidMatchesTheBytesWritten(Guid value)
            => AssertBodyLength<HasGuid>(value, BclHelpers.MeasureGuid(value));

        [Theory, MemberData(nameof(Decimals))]
        public void MeasureDecimalMatchesTheBytesWritten(decimal value)
            => AssertBodyLength<HasDecimal>(value, BclHelpers.MeasureDecimal(value));

        [Theory, MemberData(nameof(TimeSpans))]
        public void MeasureTimeSpanMatchesTheBytesWritten(TimeSpan value)
            => AssertBodyLength<HasTimeSpan>(value, BclHelpers.MeasureTimeSpan(value));

        [Theory, MemberData(nameof(DateTimes))]
        public void MeasureDateTimeMatchesTheBytesWritten(DateTime value)
            => AssertBodyLength<HasDateTime>(value, BclHelpers.MeasureDateTime(value));

        /// <summary>
        /// Serializes a one-member contract and pulls the length prefix back out, so the assertion
        /// is against protobuf-net's own output rather than a restatement of the measure.
        /// </summary>
        private static void AssertBodyLength<TContract>(object value, int measured)
            where TContract : class, new()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TContract), true);
            var obj = new TContract();
            typeof(TContract).GetProperty("Value")!.SetValue(obj, value);

            using var ms = new MemoryStream();
            model.Serialize(ms, obj);
            var payload = ms.ToArray();

            if (payload.Length == 0)
            {
                // the member was skipped entirely (a trivial value): there is no body to compare,
                // but the measure must still agree that the body would have been empty
                Assert.Equal(0, measured);
                return;
            }

            // field 1, length-delimited: tag 0x0A, then a varint length, then the body
            Assert.Equal(0x0A, payload[0]);
            int offset = 1, shift = 0;
            long length = 0;
            while (true)
            {
                byte b = payload[offset++];
                length |= (long)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }
            Assert.Equal(length, payload.Length - offset);   // the prefix describes the rest
            Assert.Equal(length, measured);
        }
    }
}
