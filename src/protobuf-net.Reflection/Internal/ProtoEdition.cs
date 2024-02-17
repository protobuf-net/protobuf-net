namespace ProtoBuf.Internal;

// https://protobuf.dev/editions/features/
internal readonly struct ProtoEdition // right now this is a struct, but if it gets big we should be able to refactor it to a class
{
    private ProtoEdition(in ProtoEdition parent, int value, int shift, int mask = 0b1)
        => _raw = (byte)((parent._raw & ~(mask << shift)) | (value & mask) << shift);

    private readonly byte _raw; // bit-packed representation of the key features

    private ProtoEdition(EditionEnumType enumType, EditionFieldPresence fieldPresence, EditionJsonFormat jsonFormat,
        EditionMessageEncoding messageEncoding, EditionRepeatedFieldEncoding repeatedFieldEncoding, EditionUtf8Validation utf8Validation)
    {
        static int Extract(int value, int shift, int mask = 0b1) => (value & mask) << shift;

        // we can change the size of _raw at any time; right now, we only have 6 options, 5 of which are on/off, and the last only needs 3 options - so: 7 bits
        _raw = (byte)
            ( Extract((int)enumType, 0)
            | Extract((int)fieldPresence, 1, 0b11)
            | Extract((int)jsonFormat, 3)
            | Extract((int)messageEncoding, 4)
            | Extract((int)repeatedFieldEncoding, 5)
            | Extract((int)utf8Validation, 6)
            );
    }

    static int Bits(byte raw, int shift, int mask = 0b1) => (raw >> shift) & mask;

    public EditionEnumType EnumType => (EditionEnumType)Bits(_raw, 0);
    public EditionFieldPresence FieldPresence => (EditionFieldPresence)Bits(_raw, 1, 0b11);
    public EditionJsonFormat JsonFormat => (EditionJsonFormat)Bits(_raw, 3);
    public EditionMessageEncoding MessageEncoding => (EditionMessageEncoding)Bits(_raw, 4);
    public EditionRepeatedFieldEncoding RepeatedFieldEncoding => (EditionRepeatedFieldEncoding)Bits(_raw, 5);
    public EditionUtf8Validation Utf8Validation => (EditionUtf8Validation)Bits(_raw, 6);

    public ProtoEdition With(EditionEnumType value) => new(this, (int)value, 0);
    public ProtoEdition With(EditionFieldPresence value) => new(this, (int)value, 1, 0b11);
    public ProtoEdition With(EditionJsonFormat value) => new(this, (int)value, 3);
    public ProtoEdition With(EditionMessageEncoding value) => new(this, (int)value, 4);
    public ProtoEdition With(EditionRepeatedFieldEncoding value) => new(this, (int)value, 5);
    public ProtoEdition With(EditionUtf8Validation value) => new(this, (int)value, 6);


    public static ProtoEdition Edition2023 { get; } = new ProtoEdition(
        EditionEnumType.Open,
        EditionFieldPresence.Explicit,
        EditionJsonFormat.Allow,
        EditionMessageEncoding.LengthPrefixed,
        EditionRepeatedFieldEncoding.Packed,
        EditionUtf8Validation.Verify
        );
    public static ProtoEdition Proto2 { get; } = new ProtoEdition(
        EditionEnumType.Closed,
        EditionFieldPresence.Explicit,
        EditionJsonFormat.LegacyBestEffort,
        EditionMessageEncoding.LengthPrefixed, // LENGTH_PREFIXED except for groups, which default to DELIMITED
        EditionRepeatedFieldEncoding.Expanded,
        EditionUtf8Validation.None
        );
    public static ProtoEdition Proto3 { get; } = new ProtoEdition(
        EditionEnumType.Open,
        EditionFieldPresence.Implicit, // IMPLICIT unless the field has the optional label, in which case it behaves like EXPLICIT.
        EditionJsonFormat.Allow,
        EditionMessageEncoding.LengthPrefixed,
        EditionRepeatedFieldEncoding.Packed,
        EditionUtf8Validation.Verify
        );


    [ProtoContract(Name = "enum_type")]
    public enum EditionEnumType // Applicable to the following elements: File, Enum
    {
        [ProtoEnum(Name = "CLOSED")]
        Closed, // Closed enums store enum values that are out of range in the unknown field set.
        [ProtoEnum(Name = "OPEN")]
        Open, // Open enums parse out of range values into their fields directly.
    }

    [ProtoContract(Name = "field_presence")]
    public enum EditionFieldPresence // Applicable to the following elements: File, Field
    {
        [ProtoEnum(Name = "LEGACY_REQUIRED")]
        LegacyRequired, // The field is required for parsing and serialization. Any explicitly-set value is serialized onto the wire (even if it is the same as the default value).
        [ProtoEnum(Name = "EXPLICIT")]
        Explicit, // The field has explicit presence tracking. Any explicitly-set value is serialized onto the wire (even if it is the same as the default value). For singular primitive fields, has_* functions are generated for fields set to EXPLICIT.
        [ProtoEnum(Name = "IMPLICIT")]
        Implicit, // The field has no presence tracking. The default value is not serialized onto the wire (even if it is explicitly set). has_* functions are not generated for fields set to IMPLICIT.
    }

    [ProtoContract(Name = "json_format")]
    public enum EditionJsonFormat // Applicable to the following elements: File, Message, Enum
    {
        [ProtoEnum(Name = "ALLOW")]
        Allow, // A runtime must allow JSON parsing and serialization. Checks are applied at the proto level to make sure that there is a well-defined mapping to JSON.
        [ProtoEnum(Name = "LEGACY_BEST_EFFORT")]
        LegacyBestEffort, // A runtime does the best it can to parse and serialize JSON. Certain protos are allowed that can result in unspecified behavior at runtime (such as many:1 or 1:many mappings).
    }

    [ProtoContract(Name = "message_encoding")] // Applicable to the following elements: File, Field
    public enum EditionMessageEncoding
    {
        [ProtoEnum(Name = "LENGTH_PREFIXED")]
        LengthPrefixed, // Fields are encoded using the LEN wire type described in Message Structure.
        [ProtoEnum(Name = "DELIMITED")]
        Delimited, // Message-typed fields are encoded as groups.
    }

    [ProtoContract(Name = "repeated_field_encoding")] // Applicable to the following elements: File, Field
    public enum EditionRepeatedFieldEncoding
    {
        [ProtoEnum(Name = "PACKED")]
        Packed, // Repeated fields of a primitive type are encoded as a single LEN record that contains each element concatenated.
        [ProtoEnum(Name = "EXPANDED")]
        Expanded, // Repeated fields are each encoded with the field number for each value.
    }

    [ProtoContract(Name = "utf8_validation")] // Applicable to the following elements: File, Field
    public enum EditionUtf8Validation
    {
        [ProtoEnum(Name = "VERIFY")]
        Verify, // The runtime should verify UTF-8. This is the default proto3 behavior.
        [ProtoEnum(Name = "NONE")]
        None, // The field behaves like an unverified bytes field on the wire. Parsers may handle this type of field in an unpredictable way, such as replacing invalid characters. This is the default proto2 behavior.
    }
}

