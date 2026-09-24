using NodaTime;
using System;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Wire-shape surrogates for the NodaTime types that protobuf-net serializes through a custom
    /// serializer rather than a contract; this type is not intended to be used directly and should
    /// be considered an implementation detail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="NodaTimeSerializers"/> is what <see cref="NodaTimeExtensions.AddNodaTime"/>
    /// installs on a <see cref="RuntimeTypeModel"/>; a compile-time model cannot use it, because a
    /// generated model has no way to reach a serializer that was chosen at runtime. These types say
    /// the same thing as ordinary contracts, so the generator can emit them, and they are paired to
    /// the NodaTime types by the assembly-level <c>[ProtoSurrogate]</c> declarations in
    /// <c>ProtoSurrogates.cs</c>.
    /// </para>
    /// <para>
    /// The field numbers and the omit-when-zero behaviour match <see cref="NodaTimeSerializers"/>
    /// exactly - the two must produce identical bytes, and <c>NodaTimeSurrogateTests</c> asserts it.
    /// They are deliberately not named as <c>.google.type.*</c>: schema generation from a
    /// compile-time model is not implemented, so a name here would describe nothing. They are
    /// structs for the same reason <see cref="WellKnownTypes.Timestamp"/> is - a surrogate is
    /// created for every value converted, and the types being surrogated are themselves structs.
    /// </para>
    /// </remarks>
    public static class NodaTimeSurrogates
    {
        /// <summary>
        /// The wire shape of <see cref="LocalDate"/>, matching <c>google.type.Date</c>.
        /// </summary>
        [ProtoContract]
        public struct Date
        {
            /// <summary>The year.</summary>
            [ProtoMember(1)] public int Year { get; set; }

            /// <summary>The month, 1-12.</summary>
            [ProtoMember(2)] public int Month { get; set; }

            /// <summary>The day of the month.</summary>
            [ProtoMember(3)] public int Day { get; set; }

            /// <summary>Convert from <see cref="LocalDate"/>.</summary>
            public static implicit operator Date(LocalDate value)
            {
                // parity with NodaTimeSerializers: the same two refusals, with the same messages
                if (value.Calendar != CalendarSystem.Iso)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Non-ISO dates cannot be converted to Protobuf Date messages. Actual calendar ID: {value.Calendar.Id}");
                }
                if (value.Year < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Dates earlier than 1AD cannot be converted to Protobuf Date messages. Year: {value.Year}");
                }
                return new Date { Year = value.Year, Month = value.Month, Day = value.Day };
            }

            /// <summary>Convert to <see cref="LocalDate"/>.</summary>
            public static implicit operator LocalDate(Date value)
            {
                // an absent field is a zero here, where the reader in NodaTimeSerializers starts
                // from the components of a default LocalDate - which are 1, 1, 1
                return new LocalDate( // ISO calendar is implicit
                    value.Year == 0 ? 1 : value.Year,
                    value.Month == 0 ? 1 : value.Month,
                    value.Day == 0 ? 1 : value.Day);
            }
        }

        /// <summary>
        /// The wire shape of <see cref="LocalTime"/>, matching <c>google.type.TimeOfDay</c>.
        /// </summary>
        [ProtoContract]
        public struct TimeOfDay
        {
            /// <summary>The hour, 0-23.</summary>
            [ProtoMember(1)] public int Hours { get; set; }

            /// <summary>The minute, 0-59.</summary>
            [ProtoMember(2)] public int Minutes { get; set; }

            /// <summary>The second, 0-59.</summary>
            [ProtoMember(3)] public int Seconds { get; set; }

            /// <summary>The nanosecond within the second.</summary>
            [ProtoMember(4)] public int Nanos { get; set; }

            /// <summary>Convert from <see cref="LocalTime"/>.</summary>
            public static implicit operator TimeOfDay(LocalTime value) => new TimeOfDay
            {
                Hours = value.Hour,
                Minutes = value.Minute,
                Seconds = value.Second,
                Nanos = value.NanosecondOfSecond,
            };

            /// <summary>Convert to <see cref="LocalTime"/>.</summary>
            public static implicit operator LocalTime(TimeOfDay value)
                => new LocalTime(value.Hours, value.Minutes, value.Seconds).PlusNanoseconds(value.Nanos);
        }
    }
}
