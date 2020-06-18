#pragma warning disable // just ... don't
using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal
{
    partial class CustomProtogenSerializer
    {
        // exported and tweaked (naming, invalid C# etc)
        private sealed class CustomProtogenSerializerServices : ISerializer<FileDescriptorSet>, ISerializer<ProtogenFileOptions>, ISerializer<ProtogenMessageOptions>, ISerializer<ProtogenFieldOptions>, ISerializer<ProtogenEnumOptions>, ISerializer<ProtogenEnumValueOptions>, ISerializer<ProtogenServiceOptions>, ISerializer<ProtogenMethodOptions>, ISerializer<ProtogenOneofOptions>, ISerializer<FileDescriptorProto>, ISerializer<DescriptorProto>, ISerializer<EnumDescriptorProto>, ISerializer<ServiceDescriptorProto>, ISerializer<FieldDescriptorProto>, ISerializer<FileOptions>, ISerializer<SourceCodeInfo>, ISerializer<DescriptorProto.ExtensionRange>, ISerializer<OneofDescriptorProto>, ISerializer<MessageOptions>, ISerializer<DescriptorProto.ReservedRange>, ISerializer<EnumValueDescriptorProto>, ISerializer<EnumOptions>, ISerializer<EnumDescriptorProto.EnumReservedRange>, ISerializer<MethodDescriptorProto>, ISerializer<ServiceOptions>, ISerializer<FieldOptions>, ISerializer<UninterpretedOption>, ISerializer<SourceCodeInfo.Location>, ISerializer<ExtensionRangeOptions>, ISerializer<OneofOptions>, ISerializer<EnumValueOptions>, ISerializer<MethodOptions>, ISerializer<UninterpretedOption.NamePart>, ISerializerProxy<Access>, ISerializerProxy<Access?>
        {
            SerializerFeatures ISerializer<DescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            DescriptorProto ISerializer<DescriptorProto>.Read(ref ProtoReader.State state, DescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new DescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    List<FieldDescriptorProto> fields;
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        fields = value.Fields;
                        RepeatedSerializer.CreateList<FieldDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, fields, this);
                        continue;
                    }
                    if (num == 3)
                    {
                        List<DescriptorProto> nestedTypes = value.NestedTypes;
                        RepeatedSerializer.CreateList<DescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, nestedTypes, this);
                        continue;
                    }
                    if (num == 4)
                    {
                        List<EnumDescriptorProto> enumTypes = value.EnumTypes;
                        RepeatedSerializer.CreateList<EnumDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, enumTypes, this);
                        continue;
                    }
                    if (num == 5)
                    {
                        List<DescriptorProto.ExtensionRange> extensionRanges = value.ExtensionRanges;
                        RepeatedSerializer.CreateList<DescriptorProto.ExtensionRange>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, extensionRanges, this);
                        continue;
                    }
                    if (num == 6)
                    {
                        fields = value.Extensions;
                        RepeatedSerializer.CreateList<FieldDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, fields, this);
                        continue;
                    }
                    if (num == 7)
                    {
                        MessageOptions options = value.Options;
                        options = state.ReadMessage<MessageOptions>(SerializerFeatures.CategoryRepeated, options, this);
                        if (options == null)
                        {
                            continue;
                        }
                        value.Options = options;
                        continue;
                    }
                    if (num == 8)
                    {
                        List<OneofDescriptorProto> oneofDecls = value.OneofDecls;
                        RepeatedSerializer.CreateList<OneofDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, oneofDecls, this);
                        continue;
                    }
                    if (num == 9)
                    {
                        List<DescriptorProto.ReservedRange> reservedRanges = value.ReservedRanges;
                        RepeatedSerializer.CreateList<DescriptorProto.ReservedRange>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, reservedRanges, this);
                        continue;
                    }
                    if (num != 10)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<string> reservedNames = value.ReservedNames;
                    RepeatedSerializer.CreateList<string>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, reservedNames, null);
                }
                return value;
            }

            void ISerializer<DescriptorProto>.Write(ref ProtoWriter.State state, DescriptorProto value)
            {
                List<FieldDescriptorProto> list;
                TypeModel.ThrowUnexpectedSubtype<DescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    string name = value.Name;
                    state.WriteString(1, name, null);
                }
                List<FieldDescriptorProto> fields = value.Fields;
                if (fields == null)
                {
                    List<FieldDescriptorProto> local1 = fields;
                }
                else
                {
                    list = fields;
                    RepeatedSerializer.CreateList<FieldDescriptorProto>().WriteRepeated(ref state, 2, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, list, this);
                }
                List<DescriptorProto> nestedTypes = value.NestedTypes;
                if (nestedTypes == null)
                {
                    List<DescriptorProto> local2 = nestedTypes;
                }
                else
                {
                    List<DescriptorProto> values = nestedTypes;
                    RepeatedSerializer.CreateList<DescriptorProto>().WriteRepeated(ref state, 3, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<EnumDescriptorProto> enumTypes = value.EnumTypes;
                if (enumTypes == null)
                {
                    List<EnumDescriptorProto> local3 = enumTypes;
                }
                else
                {
                    List<EnumDescriptorProto> values = enumTypes;
                    RepeatedSerializer.CreateList<EnumDescriptorProto>().WriteRepeated(ref state, 4, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<DescriptorProto.ExtensionRange> extensionRanges = value.ExtensionRanges;
                if (extensionRanges == null)
                {
                    List<DescriptorProto.ExtensionRange> local4 = extensionRanges;
                }
                else
                {
                    List<DescriptorProto.ExtensionRange> values = extensionRanges;
                    RepeatedSerializer.CreateList<DescriptorProto.ExtensionRange>().WriteRepeated(ref state, 5, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<FieldDescriptorProto> extensions = value.Extensions;
                if (extensions == null)
                {
                    List<FieldDescriptorProto> local5 = extensions;
                }
                else
                {
                    list = extensions;
                    RepeatedSerializer.CreateList<FieldDescriptorProto>().WriteRepeated(ref state, 6, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, list, this);
                }
                MessageOptions options = value.Options;
                state.WriteMessage<MessageOptions>(7, SerializerFeatures.CategoryRepeated, options, this);
                List<OneofDescriptorProto> oneofDecls = value.OneofDecls;
                if (oneofDecls == null)
                {
                    List<OneofDescriptorProto> local6 = oneofDecls;
                }
                else
                {
                    List<OneofDescriptorProto> values = oneofDecls;
                    RepeatedSerializer.CreateList<OneofDescriptorProto>().WriteRepeated(ref state, 8, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<DescriptorProto.ReservedRange> reservedRanges = value.ReservedRanges;
                if (reservedRanges == null)
                {
                    List<DescriptorProto.ReservedRange> local7 = reservedRanges;
                }
                else
                {
                    List<DescriptorProto.ReservedRange> values = reservedRanges;
                    RepeatedSerializer.CreateList<DescriptorProto.ReservedRange>().WriteRepeated(ref state, 9, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<string> reservedNames = value.ReservedNames;
                if (reservedNames == null)
                {
                    List<string> local8 = reservedNames;
                }
                else
                {
                    List<string> values = reservedNames;
                    RepeatedSerializer.CreateList<string>().WriteRepeated(ref state, 10, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, null);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<DescriptorProto.ExtensionRange>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            DescriptorProto.ExtensionRange ISerializer<DescriptorProto.ExtensionRange>.Read(ref ProtoReader.State state, DescriptorProto.ExtensionRange value)
            {
                int num;
                if (value == null)
                {
                    value = new DescriptorProto.ExtensionRange();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    int num2;
                    if (num == 1)
                    {
                        num2 = state.ReadInt32();
                        value.Start = num2;
                        continue;
                    }
                    if (num == 2)
                    {
                        num2 = state.ReadInt32();
                        value.End = num2;
                        continue;
                    }
                    if (num != 3)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    ExtensionRangeOptions options = value.Options;
                    options = state.ReadMessage<ExtensionRangeOptions>(SerializerFeatures.CategoryRepeated, options, this);
                    if (options != null)
                    {
                        value.Options = options;
                    }
                }
                return value;
            }

            void ISerializer<DescriptorProto.ExtensionRange>.Write(ref ProtoWriter.State state, DescriptorProto.ExtensionRange value)
            {
                int start;
                TypeModel.ThrowUnexpectedSubtype<DescriptorProto.ExtensionRange>(value);
                if (value.ShouldSerializeStart())
                {
                    start = value.Start;
                    state.WriteInt32Varint(1, start);
                }
                if (value.ShouldSerializeEnd())
                {
                    start = value.End;
                    state.WriteInt32Varint(2, start);
                }
                ExtensionRangeOptions options = value.Options;
                state.WriteMessage<ExtensionRangeOptions>(3, SerializerFeatures.CategoryRepeated, options, this);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<DescriptorProto.ReservedRange>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            DescriptorProto.ReservedRange ISerializer<DescriptorProto.ReservedRange>.Read(ref ProtoReader.State state, DescriptorProto.ReservedRange value)
            {
                int num;
                if (value == null)
                {
                    value = new DescriptorProto.ReservedRange();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    int num2;
                    if (num == 1)
                    {
                        num2 = state.ReadInt32();
                        value.Start = num2;
                        continue;
                    }
                    if (num != 2)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    num2 = state.ReadInt32();
                    value.End = num2;
                }
                return value;
            }

            void ISerializer<DescriptorProto.ReservedRange>.Write(ref ProtoWriter.State state, DescriptorProto.ReservedRange value)
            {
                int start;
                TypeModel.ThrowUnexpectedSubtype<DescriptorProto.ReservedRange>(value);
                if (value.ShouldSerializeStart())
                {
                    start = value.Start;
                    state.WriteInt32Varint(1, start);
                }
                if (value.ShouldSerializeEnd())
                {
                    start = value.End;
                    state.WriteInt32Varint(2, start);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<EnumDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            EnumDescriptorProto ISerializer<EnumDescriptorProto>.Read(ref ProtoReader.State state, EnumDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new EnumDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        List<EnumValueDescriptorProto> values = value.Values;
                        RepeatedSerializer.CreateList<EnumValueDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                        continue;
                    }
                    if (num == 3)
                    {
                        EnumOptions options = value.Options;
                        options = state.ReadMessage<EnumOptions>(SerializerFeatures.CategoryRepeated, options, this);
                        if (options == null)
                        {
                            continue;
                        }
                        value.Options = options;
                        continue;
                    }
                    if (num == 4)
                    {
                        List<EnumDescriptorProto.EnumReservedRange> reservedRanges = value.ReservedRanges;
                        RepeatedSerializer.CreateList<EnumDescriptorProto.EnumReservedRange>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, reservedRanges, this);
                        continue;
                    }
                    if (num != 5)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<string> reservedNames = value.ReservedNames;
                    RepeatedSerializer.CreateList<string>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, reservedNames, null);
                }
                return value;
            }

            void ISerializer<EnumDescriptorProto>.Write(ref ProtoWriter.State state, EnumDescriptorProto value)
            {
                TypeModel.ThrowUnexpectedSubtype<EnumDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    string name = value.Name;
                    state.WriteString(1, name, null);
                }
                List<EnumValueDescriptorProto> values = value.Values;
                if (values == null)
                {
                    List<EnumValueDescriptorProto> local1 = values;
                }
                else
                {
                    List<EnumValueDescriptorProto> list = values;
                    RepeatedSerializer.CreateList<EnumValueDescriptorProto>().WriteRepeated(ref state, 2, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, list, this);
                }
                EnumOptions options = value.Options;
                state.WriteMessage<EnumOptions>(3, SerializerFeatures.CategoryRepeated, options, this);
                List<EnumDescriptorProto.EnumReservedRange> reservedRanges = value.ReservedRanges;
                if (reservedRanges == null)
                {
                    List<EnumDescriptorProto.EnumReservedRange> local2 = reservedRanges;
                }
                else
                {
                    List<EnumDescriptorProto.EnumReservedRange> list2 = reservedRanges;
                    RepeatedSerializer.CreateList<EnumDescriptorProto.EnumReservedRange>().WriteRepeated(ref state, 4, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, list2, this);
                }
                List<string> reservedNames = value.ReservedNames;
                if (reservedNames == null)
                {
                    List<string> local3 = reservedNames;
                }
                else
                {
                    List<string> list3 = reservedNames;
                    RepeatedSerializer.CreateList<string>().WriteRepeated(ref state, 5, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, list3, null);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<EnumDescriptorProto.EnumReservedRange>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            EnumDescriptorProto.EnumReservedRange ISerializer<EnumDescriptorProto.EnumReservedRange>.Read(ref ProtoReader.State state, EnumDescriptorProto.EnumReservedRange value)
            {
                int num;
                if (value == null)
                {
                    value = new EnumDescriptorProto.EnumReservedRange();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    int num2;
                    if (num == 1)
                    {
                        num2 = state.ReadInt32();
                        value.Start = num2;
                        continue;
                    }
                    if (num != 2)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    num2 = state.ReadInt32();
                    value.End = num2;
                }
                return value;
            }

            void ISerializer<EnumDescriptorProto.EnumReservedRange>.Write(ref ProtoWriter.State state, EnumDescriptorProto.EnumReservedRange value)
            {
                int start;
                TypeModel.ThrowUnexpectedSubtype<EnumDescriptorProto.EnumReservedRange>(value);
                if (value.ShouldSerializeStart())
                {
                    start = value.Start;
                    state.WriteInt32Varint(1, start);
                }
                if (value.ShouldSerializeEnd())
                {
                    start = value.End;
                    state.WriteInt32Varint(2, start);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<EnumOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            EnumOptions ISerializer<EnumOptions>.Read(ref ProtoReader.State state, EnumOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new EnumOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    bool flag;
                    if (num == 2)
                    {
                        flag = state.ReadBoolean();
                        value.AllowAlias = flag;
                        continue;
                    }
                    if (num == 3)
                    {
                        flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<EnumOptions>.Write(ref ProtoWriter.State state, EnumOptions value)
            {
                bool allowAlias;
                TypeModel.ThrowUnexpectedSubtype<EnumOptions>(value);
                if (value.ShouldSerializeAllowAlias())
                {
                    state.WriteFieldHeader(2, WireType.Variant);
                    allowAlias = value.AllowAlias;
                    state.WriteBoolean(allowAlias);
                }
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(3, WireType.Variant);
                    allowAlias = value.Deprecated;
                    state.WriteBoolean(allowAlias);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<EnumValueDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            EnumValueDescriptorProto ISerializer<EnumValueDescriptorProto>.Read(ref ProtoReader.State state, EnumValueDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new EnumValueDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        int num2 = state.ReadInt32();
                        value.Number = num2;
                        continue;
                    }
                    if (num != 3)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    EnumValueOptions options = value.Options;
                    options = state.ReadMessage<EnumValueOptions>(SerializerFeatures.CategoryRepeated, options, this);
                    if (options != null)
                    {
                        value.Options = options;
                    }
                }
                return value;
            }

            void ISerializer<EnumValueDescriptorProto>.Write(ref ProtoWriter.State state, EnumValueDescriptorProto value)
            {
                TypeModel.ThrowUnexpectedSubtype<EnumValueDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    string name = value.Name;
                    state.WriteString(1, name, null);
                }
                if (value.ShouldSerializeNumber())
                {
                    int number = value.Number;
                    state.WriteInt32Varint(2, number);
                }
                EnumValueOptions options = value.Options;
                state.WriteMessage<EnumValueOptions>(3, SerializerFeatures.CategoryRepeated, options, this);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<EnumValueOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            EnumValueOptions ISerializer<EnumValueOptions>.Read(ref ProtoReader.State state, EnumValueOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new EnumValueOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        bool flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<EnumValueOptions>.Write(ref ProtoWriter.State state, EnumValueOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<EnumValueOptions>(value);
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(1, WireType.Variant);
                    bool deprecated = value.Deprecated;
                    state.WriteBoolean(deprecated);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ExtensionRangeOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ExtensionRangeOptions ISerializer<ExtensionRangeOptions>.Read(ref ProtoReader.State state, ExtensionRangeOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ExtensionRangeOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<ExtensionRangeOptions>.Write(ref ProtoWriter.State state, ExtensionRangeOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ExtensionRangeOptions>(value);
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<FieldDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            FieldDescriptorProto ISerializer<FieldDescriptorProto>.Read(ref ProtoReader.State state, FieldDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new FieldDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    int num2;
                    if (num == 1)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Extendee = str;
                        continue;
                    }
                    if (num == 3)
                    {
                        num2 = state.ReadInt32();
                        value.Number = num2;
                        continue;
                    }
                    if (num == 4)
                    {
                        FieldDescriptorProto.Label label = (FieldDescriptorProto.Label)state.ReadInt32();
                        value.label = label;
                        continue;
                    }
                    if (num == 5)
                    {
                        FieldDescriptorProto.Type type = (FieldDescriptorProto.Type)state.ReadInt32();
                        value.type = type;
                        continue;
                    }
                    if (num == 6)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.TypeName = str;
                        continue;
                    }
                    if (num == 7)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.DefaultValue = str;
                        continue;
                    }
                    if (num == 8)
                    {
                        FieldOptions options = value.Options;
                        options = state.ReadMessage<FieldOptions>(SerializerFeatures.CategoryRepeated, options, this);
                        if (options == null)
                        {
                            continue;
                        }
                        value.Options = options;
                        continue;
                    }
                    if (num == 9)
                    {
                        num2 = state.ReadInt32();
                        value.OneofIndex = num2;
                        continue;
                    }
                    if (num == 17)
                    {
                        num2 = state.ReadInt32();
                        value.Proto3Optional = num2 != 0;
                        continue;
                    }
                    if (num != 10)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    str = state.ReadString(null);
                    if (str != null)
                    {
                        value.JsonName = str;
                    }
                }
                return value;
            }

            void ISerializer<FieldDescriptorProto>.Write(ref ProtoWriter.State state, FieldDescriptorProto value)
            {
                string name;
                int number;
                TypeModel.ThrowUnexpectedSubtype<FieldDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    name = value.Name;
                    state.WriteString(1, name, null);
                }
                if (value.ShouldSerializeExtendee())
                {
                    name = value.Extendee;
                    state.WriteString(2, name, null);
                }
                if (value.ShouldSerializeNumber())
                {
                    number = value.Number;
                    state.WriteInt32Varint(3, number);
                }
                if (value.ShouldSerializelabel())
                {
                    number = (int)value.label;
                    state.WriteInt32Varint(4, number);
                }
                if (value.ShouldSerializetype())
                {
                    number = (int)value.type;
                    state.WriteInt32Varint(5, number);
                }
                if (value.ShouldSerializeTypeName())
                {
                    name = value.TypeName;
                    state.WriteString(6, name, null);
                }
                if (value.ShouldSerializeDefaultValue())
                {
                    name = value.DefaultValue;
                    state.WriteString(7, name, null);
                }
                FieldOptions options = value.Options;
                state.WriteMessage<FieldOptions>(8, SerializerFeatures.CategoryRepeated, options, this);
                if (value.ShouldSerializeOneofIndex())
                {
                    number = value.OneofIndex;
                    state.WriteInt32Varint(9, number);
                }
                if (value.ShouldSerializeJsonName())
                {
                    name = value.JsonName;
                    state.WriteString(10, name, null);
                }
                if (value.ShouldSerializeProto3Optional())
                {
                    number = value.Proto3Optional ? 1 : 0;
                    state.WriteInt32Varint(17, number);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<FieldOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            FieldOptions ISerializer<FieldOptions>.Read(ref ProtoReader.State state, FieldOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new FieldOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    bool flag;
                    if (num == 1)
                    {
                        FieldOptions.CType type = (FieldOptions.CType)state.ReadInt32();
                        value.Ctype = type;
                        continue;
                    }
                    if (num == 2)
                    {
                        flag = state.ReadBoolean();
                        value.Packed = flag;
                        continue;
                    }
                    if (num == 3)
                    {
                        flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num == 5)
                    {
                        flag = state.ReadBoolean();
                        value.Lazy = flag;
                        continue;
                    }
                    if (num == 6)
                    {
                        FieldOptions.JSType type2 = (FieldOptions.JSType)state.ReadInt32();
                        value.Jstype = type2;
                        continue;
                    }
                    if (num == 10)
                    {
                        flag = state.ReadBoolean();
                        value.Weak = flag;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<FieldOptions>.Write(ref ProtoWriter.State state, FieldOptions value)
            {
                int ctype;
                bool packed;
                TypeModel.ThrowUnexpectedSubtype<FieldOptions>(value);
                if (value.ShouldSerializeCtype())
                {
                    ctype = (int)value.Ctype;
                    state.WriteInt32Varint(1, ctype);
                }
                if (value.ShouldSerializePacked())
                {
                    state.WriteFieldHeader(2, WireType.Variant);
                    packed = value.Packed;
                    state.WriteBoolean(packed);
                }
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(3, WireType.Variant);
                    packed = value.Deprecated;
                    state.WriteBoolean(packed);
                }
                if (value.ShouldSerializeLazy())
                {
                    state.WriteFieldHeader(5, WireType.Variant);
                    packed = value.Lazy;
                    state.WriteBoolean(packed);
                }
                if (value.ShouldSerializeJstype())
                {
                    ctype = (int)value.Jstype;
                    state.WriteInt32Varint(6, ctype);
                }
                if (value.ShouldSerializeWeak())
                {
                    state.WriteFieldHeader(10, WireType.Variant);
                    packed = value.Weak;
                    state.WriteBoolean(packed);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<FileDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            FileDescriptorProto ISerializer<FileDescriptorProto>.Read(ref ProtoReader.State state, FileDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new FileDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    int[] publicDependencies;
                    if (num == 1)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Package = str;
                        continue;
                    }
                    if (num == 3)
                    {
                        List<string> dependencies = value.Dependencies;
                        RepeatedSerializer.CreateList<string>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, dependencies, null);
                        continue;
                    }
                    if (num == 4)
                    {
                        List<DescriptorProto> messageTypes = value.MessageTypes;
                        RepeatedSerializer.CreateList<DescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, messageTypes, this);
                        continue;
                    }
                    if (num == 5)
                    {
                        List<EnumDescriptorProto> enumTypes = value.EnumTypes;
                        RepeatedSerializer.CreateList<EnumDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, enumTypes, this);
                        continue;
                    }
                    if (num == 6)
                    {
                        List<ServiceDescriptorProto> services = value.Services;
                        RepeatedSerializer.CreateList<ServiceDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, services, this);
                        continue;
                    }
                    if (num == 7)
                    {
                        List<FieldDescriptorProto> extensions = value.Extensions;
                        RepeatedSerializer.CreateList<FieldDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, extensions, this);
                        continue;
                    }
                    if (num == 8)
                    {
                        FileOptions options = value.Options;
                        options = state.ReadMessage<FileOptions>(SerializerFeatures.CategoryRepeated, options, this);
                        if (options == null)
                        {
                            continue;
                        }
                        value.Options = options;
                        continue;
                    }
                    if (num == 9)
                    {
                        SourceCodeInfo sourceCodeInfo = value.SourceCodeInfo;
                        sourceCodeInfo = state.ReadMessage<SourceCodeInfo>(SerializerFeatures.CategoryRepeated, sourceCodeInfo, this);
                        if (sourceCodeInfo == null)
                        {
                            continue;
                        }
                        value.SourceCodeInfo = sourceCodeInfo;
                        continue;
                    }
                    if (num == 10)
                    {
                        publicDependencies = value.PublicDependencies;
                        publicDependencies = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeSpecified, publicDependencies, null);
                        if (publicDependencies == null)
                        {
                            continue;
                        }
                        value.PublicDependencies = publicDependencies;
                        continue;
                    }
                    if (num == 11)
                    {
                        publicDependencies = value.WeakDependencies;
                        publicDependencies = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeSpecified, publicDependencies, null);
                        if (publicDependencies == null)
                        {
                            continue;
                        }
                        value.WeakDependencies = publicDependencies;
                        continue;
                    }
                    if (num != 12)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    str = state.ReadString(null);
                    if (str != null)
                    {
                        value.Syntax = str;
                    }
                }
                return value;
            }

            void ISerializer<FileDescriptorProto>.Write(ref ProtoWriter.State state, FileDescriptorProto value)
            {
                string name;
                int[] numArray;
                TypeModel.ThrowUnexpectedSubtype<FileDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    name = value.Name;
                    state.WriteString(1, name, null);
                }
                if (value.ShouldSerializePackage())
                {
                    name = value.Package;
                    state.WriteString(2, name, null);
                }
                List<string> dependencies = value.Dependencies;
                if (dependencies == null)
                {
                    List<string> local1 = dependencies;
                }
                else
                {
                    List<string> values = dependencies;
                    RepeatedSerializer.CreateList<string>().WriteRepeated(ref state, 3, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, null);
                }
                List<DescriptorProto> messageTypes = value.MessageTypes;
                if (messageTypes == null)
                {
                    List<DescriptorProto> local2 = messageTypes;
                }
                else
                {
                    List<DescriptorProto> values = messageTypes;
                    RepeatedSerializer.CreateList<DescriptorProto>().WriteRepeated(ref state, 4, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<EnumDescriptorProto> enumTypes = value.EnumTypes;
                if (enumTypes == null)
                {
                    List<EnumDescriptorProto> local3 = enumTypes;
                }
                else
                {
                    List<EnumDescriptorProto> values = enumTypes;
                    RepeatedSerializer.CreateList<EnumDescriptorProto>().WriteRepeated(ref state, 5, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<ServiceDescriptorProto> services = value.Services;
                if (services == null)
                {
                    List<ServiceDescriptorProto> local4 = services;
                }
                else
                {
                    List<ServiceDescriptorProto> values = services;
                    RepeatedSerializer.CreateList<ServiceDescriptorProto>().WriteRepeated(ref state, 6, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                List<FieldDescriptorProto> extensions = value.Extensions;
                if (extensions == null)
                {
                    List<FieldDescriptorProto> local5 = extensions;
                }
                else
                {
                    List<FieldDescriptorProto> values = extensions;
                    RepeatedSerializer.CreateList<FieldDescriptorProto>().WriteRepeated(ref state, 7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                FileOptions options = value.Options;
                state.WriteMessage<FileOptions>(8, SerializerFeatures.CategoryRepeated, options, this);
                SourceCodeInfo sourceCodeInfo = value.SourceCodeInfo;
                state.WriteMessage<SourceCodeInfo>(9, SerializerFeatures.CategoryRepeated, sourceCodeInfo, this);
                int[] publicDependencies = value.PublicDependencies;
                if (publicDependencies == null)
                {
                    int[] local6 = publicDependencies;
                }
                else
                {
                    numArray = publicDependencies;
                    RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 10, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeSpecified, numArray, null);
                }
                int[] weakDependencies = value.WeakDependencies;
                if (weakDependencies == null)
                {
                    int[] local7 = weakDependencies;
                }
                else
                {
                    numArray = weakDependencies;
                    RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 11, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeSpecified, numArray, null);
                }
                if (value.ShouldSerializeSyntax())
                {
                    name = value.Syntax;
                    state.WriteString(12, name, null);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<FileDescriptorSet>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            FileDescriptorSet ISerializer<FileDescriptorSet>.Read(ref ProtoReader.State state, FileDescriptorSet value)
            {
                int num;
                if (value == null)
                {
                    value = new FileDescriptorSet();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num != 1)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<FileDescriptorProto> files = value.Files;
                    RepeatedSerializer.CreateList<FileDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, files, this);
                }
                return value;
            }

            void ISerializer<FileDescriptorSet>.Write(ref ProtoWriter.State state, FileDescriptorSet value)
            {
                TypeModel.ThrowUnexpectedSubtype<FileDescriptorSet>(value);
                List<FileDescriptorProto> files = value.Files;
                if (files == null)
                {
                    List<FileDescriptorProto> local1 = files;
                }
                else
                {
                    List<FileDescriptorProto> values = files;
                    RepeatedSerializer.CreateList<FileDescriptorProto>().WriteRepeated(ref state, 1, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<FileOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            FileOptions ISerializer<FileOptions>.Read(ref ProtoReader.State state, FileOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new FileOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    bool flag;
                    if (num == 1)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.JavaPackage = str;
                        continue;
                    }
                    if (num == 8)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.JavaOuterClassname = str;
                        continue;
                    }
                    if (num == 9)
                    {
                        FileOptions.OptimizeMode mode = (FileOptions.OptimizeMode)state.ReadInt32();
                        value.OptimizeFor = mode;
                        continue;
                    }
                    if (num == 10)
                    {
                        flag = state.ReadBoolean();
                        value.JavaMultipleFiles = flag;
                        continue;
                    }
                    if (num == 11)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.GoPackage = str;
                        continue;
                    }
                    if (num == 0x10)
                    {
                        flag = state.ReadBoolean();
                        value.CcGenericServices = flag;
                        continue;
                    }
                    if (num == 0x11)
                    {
                        flag = state.ReadBoolean();
                        value.JavaGenericServices = flag;
                        continue;
                    }
                    if (num == 0x12)
                    {
                        flag = state.ReadBoolean();
                        value.PyGenericServices = flag;
                        continue;
                    }
                    if (num == 20)
                    {
                        flag = state.ReadBoolean();
                        value.JavaGenerateEqualsAndHash = flag;
                        continue;
                    }
                    if (num == 0x17)
                    {
                        flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num == 0x1b)
                    {
                        flag = state.ReadBoolean();
                        value.JavaStringCheckUtf8 = flag;
                        continue;
                    }
                    if (num == 0x1f)
                    {
                        flag = state.ReadBoolean();
                        value.CcEnableArenas = flag;
                        continue;
                    }
                    if (num == 0x24)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.ObjcClassPrefix = str;
                        continue;
                    }
                    if (num == 0x25)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.CsharpNamespace = str;
                        continue;
                    }
                    if (num == 0x27)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.SwiftPrefix = str;
                        continue;
                    }
                    if (num == 40)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.PhpClassPrefix = str;
                        continue;
                    }
                    if (num == 0x29)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.PhpNamespace = str;
                        continue;
                    }
                    if (num == 0x2a)
                    {
                        flag = state.ReadBoolean();
                        value.PhpGenericServices = flag;
                        continue;
                    }
                    if (num == 0x2c)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.PhpMetadataNamespace = str;
                        continue;
                    }
                    if (num == 0x2d)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.RubyPackage = str;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<FileOptions>.Write(ref ProtoWriter.State state, FileOptions value)
            {
                string javaPackage;
                bool javaMultipleFiles;
                TypeModel.ThrowUnexpectedSubtype<FileOptions>(value);
                if (value.ShouldSerializeJavaPackage())
                {
                    javaPackage = value.JavaPackage;
                    state.WriteString(1, javaPackage, null);
                }
                if (value.ShouldSerializeJavaOuterClassname())
                {
                    javaPackage = value.JavaOuterClassname;
                    state.WriteString(8, javaPackage, null);
                }
                if (value.ShouldSerializeOptimizeFor())
                {
                    int optimizeFor = (int)value.OptimizeFor;
                    state.WriteInt32Varint(9, optimizeFor);
                }
                if (value.ShouldSerializeJavaMultipleFiles())
                {
                    state.WriteFieldHeader(10, WireType.Variant);
                    javaMultipleFiles = value.JavaMultipleFiles;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeGoPackage())
                {
                    javaPackage = value.GoPackage;
                    state.WriteString(11, javaPackage, null);
                }
                if (value.ShouldSerializeCcGenericServices())
                {
                    state.WriteFieldHeader(0x10, WireType.Variant);
                    javaMultipleFiles = value.CcGenericServices;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeJavaGenericServices())
                {
                    state.WriteFieldHeader(0x11, WireType.Variant);
                    javaMultipleFiles = value.JavaGenericServices;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializePyGenericServices())
                {
                    state.WriteFieldHeader(0x12, WireType.Variant);
                    javaMultipleFiles = value.PyGenericServices;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeJavaGenerateEqualsAndHash())
                {
                    state.WriteFieldHeader(20, WireType.Variant);
                    javaMultipleFiles = value.JavaGenerateEqualsAndHash;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(0x17, WireType.Variant);
                    javaMultipleFiles = value.Deprecated;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeJavaStringCheckUtf8())
                {
                    state.WriteFieldHeader(0x1b, WireType.Variant);
                    javaMultipleFiles = value.JavaStringCheckUtf8;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeCcEnableArenas())
                {
                    state.WriteFieldHeader(0x1f, WireType.Variant);
                    javaMultipleFiles = value.CcEnableArenas;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializeObjcClassPrefix())
                {
                    javaPackage = value.ObjcClassPrefix;
                    state.WriteString(0x24, javaPackage, null);
                }
                if (value.ShouldSerializeCsharpNamespace())
                {
                    javaPackage = value.CsharpNamespace;
                    state.WriteString(0x25, javaPackage, null);
                }
                if (value.ShouldSerializeSwiftPrefix())
                {
                    javaPackage = value.SwiftPrefix;
                    state.WriteString(0x27, javaPackage, null);
                }
                if (value.ShouldSerializePhpClassPrefix())
                {
                    javaPackage = value.PhpClassPrefix;
                    state.WriteString(40, javaPackage, null);
                }
                if (value.ShouldSerializePhpNamespace())
                {
                    javaPackage = value.PhpNamespace;
                    state.WriteString(0x29, javaPackage, null);
                }
                if (value.ShouldSerializePhpGenericServices())
                {
                    state.WriteFieldHeader(0x2a, WireType.Variant);
                    javaMultipleFiles = value.PhpGenericServices;
                    state.WriteBoolean(javaMultipleFiles);
                }
                if (value.ShouldSerializePhpMetadataNamespace())
                {
                    javaPackage = value.PhpMetadataNamespace;
                    state.WriteString(0x2c, javaPackage, null);
                }
                if (value.ShouldSerializeRubyPackage())
                {
                    javaPackage = value.RubyPackage;
                    state.WriteString(0x2d, javaPackage, null);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<MessageOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            MessageOptions ISerializer<MessageOptions>.Read(ref ProtoReader.State state, MessageOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new MessageOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    bool flag;
                    if (num == 1)
                    {
                        flag = state.ReadBoolean();
                        value.MessageSetWireFormat = flag;
                        continue;
                    }
                    if (num == 2)
                    {
                        flag = state.ReadBoolean();
                        value.NoStandardDescriptorAccessor = flag;
                        continue;
                    }
                    if (num == 3)
                    {
                        flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num == 7)
                    {
                        flag = state.ReadBoolean();
                        value.MapEntry = flag;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<MessageOptions>.Write(ref ProtoWriter.State state, MessageOptions value)
            {
                bool messageSetWireFormat;
                TypeModel.ThrowUnexpectedSubtype<MessageOptions>(value);
                if (value.ShouldSerializeMessageSetWireFormat())
                {
                    state.WriteFieldHeader(1, WireType.Variant);
                    messageSetWireFormat = value.MessageSetWireFormat;
                    state.WriteBoolean(messageSetWireFormat);
                }
                if (value.ShouldSerializeNoStandardDescriptorAccessor())
                {
                    state.WriteFieldHeader(2, WireType.Variant);
                    messageSetWireFormat = value.NoStandardDescriptorAccessor;
                    state.WriteBoolean(messageSetWireFormat);
                }
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(3, WireType.Variant);
                    messageSetWireFormat = value.Deprecated;
                    state.WriteBoolean(messageSetWireFormat);
                }
                if (value.ShouldSerializeMapEntry())
                {
                    state.WriteFieldHeader(7, WireType.Variant);
                    messageSetWireFormat = value.MapEntry;
                    state.WriteBoolean(messageSetWireFormat);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<MethodDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            MethodDescriptorProto ISerializer<MethodDescriptorProto>.Read(ref ProtoReader.State state, MethodDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new MethodDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    bool flag;
                    if (num == 1)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.InputType = str;
                        continue;
                    }
                    if (num == 3)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.OutputType = str;
                        continue;
                    }
                    if (num == 4)
                    {
                        MethodOptions options = value.Options;
                        options = state.ReadMessage<MethodOptions>(SerializerFeatures.CategoryRepeated, options, this);
                        if (options == null)
                        {
                            continue;
                        }
                        value.Options = options;
                        continue;
                    }
                    if (num == 5)
                    {
                        flag = state.ReadBoolean();
                        value.ClientStreaming = flag;
                        continue;
                    }
                    if (num != 6)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    flag = state.ReadBoolean();
                    value.ServerStreaming = flag;
                }
                return value;
            }

            void ISerializer<MethodDescriptorProto>.Write(ref ProtoWriter.State state, MethodDescriptorProto value)
            {
                string name;
                bool clientStreaming;
                TypeModel.ThrowUnexpectedSubtype<MethodDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    name = value.Name;
                    state.WriteString(1, name, null);
                }
                if (value.ShouldSerializeInputType())
                {
                    name = value.InputType;
                    state.WriteString(2, name, null);
                }
                if (value.ShouldSerializeOutputType())
                {
                    name = value.OutputType;
                    state.WriteString(3, name, null);
                }
                MethodOptions options = value.Options;
                state.WriteMessage<MethodOptions>(4, SerializerFeatures.CategoryRepeated, options, this);
                if (value.ShouldSerializeClientStreaming())
                {
                    state.WriteFieldHeader(5, WireType.Variant);
                    clientStreaming = value.ClientStreaming;
                    state.WriteBoolean(clientStreaming);
                }
                if (value.ShouldSerializeServerStreaming())
                {
                    state.WriteFieldHeader(6, WireType.Variant);
                    clientStreaming = value.ServerStreaming;
                    state.WriteBoolean(clientStreaming);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<MethodOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            MethodOptions ISerializer<MethodOptions>.Read(ref ProtoReader.State state, MethodOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new MethodOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 0x21)
                    {
                        bool flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num == 0x22)
                    {
                        MethodOptions.IdempotencyLevel level = (MethodOptions.IdempotencyLevel)state.ReadInt32();
                        value.idempotency_level = level;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<MethodOptions>.Write(ref ProtoWriter.State state, MethodOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<MethodOptions>(value);
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(0x21, WireType.Variant);
                    bool deprecated = value.Deprecated;
                    state.WriteBoolean(deprecated);
                }
                if (value.ShouldSerializeidempotency_level())
                {
                    int num = (int)value.idempotency_level;
                    state.WriteInt32Varint(0x22, num);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<OneofDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            OneofDescriptorProto ISerializer<OneofDescriptorProto>.Read(ref ProtoReader.State state, OneofDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new OneofDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num != 2)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    OneofOptions options = value.Options;
                    options = state.ReadMessage<OneofOptions>(SerializerFeatures.CategoryRepeated, options, this);
                    if (options != null)
                    {
                        value.Options = options;
                    }
                }
                return value;
            }

            void ISerializer<OneofDescriptorProto>.Write(ref ProtoWriter.State state, OneofDescriptorProto value)
            {
                TypeModel.ThrowUnexpectedSubtype<OneofDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    string name = value.Name;
                    state.WriteString(1, name, null);
                }
                OneofOptions options = value.Options;
                state.WriteMessage<OneofOptions>(2, SerializerFeatures.CategoryRepeated, options, this);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<OneofOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            OneofOptions ISerializer<OneofOptions>.Read(ref ProtoReader.State state, OneofOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new OneofOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<OneofOptions>.Write(ref ProtoWriter.State state, OneofOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<OneofOptions>(value);
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ServiceDescriptorProto>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ServiceDescriptorProto ISerializer<ServiceDescriptorProto>.Read(ref ProtoReader.State state, ServiceDescriptorProto value)
            {
                int num;
                if (value == null)
                {
                    value = new ServiceDescriptorProto();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        List<MethodDescriptorProto> methods = value.Methods;
                        RepeatedSerializer.CreateList<MethodDescriptorProto>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, methods, this);
                        continue;
                    }
                    if (num != 3)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    ServiceOptions options = value.Options;
                    options = state.ReadMessage<ServiceOptions>(SerializerFeatures.CategoryRepeated, options, this);
                    if (options != null)
                    {
                        value.Options = options;
                    }
                }
                return value;
            }

            void ISerializer<ServiceDescriptorProto>.Write(ref ProtoWriter.State state, ServiceDescriptorProto value)
            {
                TypeModel.ThrowUnexpectedSubtype<ServiceDescriptorProto>(value);
                if (value.ShouldSerializeName())
                {
                    string name = value.Name;
                    state.WriteString(1, name, null);
                }
                List<MethodDescriptorProto> methods = value.Methods;
                if (methods == null)
                {
                    List<MethodDescriptorProto> local1 = methods;
                }
                else
                {
                    List<MethodDescriptorProto> values = methods;
                    RepeatedSerializer.CreateList<MethodDescriptorProto>().WriteRepeated(ref state, 2, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                ServiceOptions options = value.Options;
                state.WriteMessage<ServiceOptions>(3, SerializerFeatures.CategoryRepeated, options, this);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ServiceOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ServiceOptions ISerializer<ServiceOptions>.Read(ref ProtoReader.State state, ServiceOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ServiceOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 0x21)
                    {
                        bool flag = state.ReadBoolean();
                        value.Deprecated = flag;
                        continue;
                    }
                    if (num != 0x3e7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, uninterpretedOptions, this);
                }
                return value;
            }

            void ISerializer<ServiceOptions>.Write(ref ProtoWriter.State state, ServiceOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ServiceOptions>(value);
                if (value.ShouldSerializeDeprecated())
                {
                    state.WriteFieldHeader(0x21, WireType.Variant);
                    bool deprecated = value.Deprecated;
                    state.WriteBoolean(deprecated);
                }
                List<UninterpretedOption> uninterpretedOptions = value.UninterpretedOptions;
                if (uninterpretedOptions == null)
                {
                    List<UninterpretedOption> local1 = uninterpretedOptions;
                }
                else
                {
                    List<UninterpretedOption> values = uninterpretedOptions;
                    RepeatedSerializer.CreateList<UninterpretedOption>().WriteRepeated(ref state, 0x3e7, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<SourceCodeInfo>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            SourceCodeInfo ISerializer<SourceCodeInfo>.Read(ref ProtoReader.State state, SourceCodeInfo value)
            {
                int num;
                if (value == null)
                {
                    value = new SourceCodeInfo();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num != 1)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<SourceCodeInfo.Location> locations = value.Locations;
                    RepeatedSerializer.CreateList<SourceCodeInfo.Location>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, locations, this);
                }
                return value;
            }

            void ISerializer<SourceCodeInfo>.Write(ref ProtoWriter.State state, SourceCodeInfo value)
            {
                TypeModel.ThrowUnexpectedSubtype<SourceCodeInfo>(value);
                List<SourceCodeInfo.Location> locations = value.Locations;
                if (locations == null)
                {
                    List<SourceCodeInfo.Location> local1 = locations;
                }
                else
                {
                    List<SourceCodeInfo.Location> values = locations;
                    RepeatedSerializer.CreateList<SourceCodeInfo.Location>().WriteRepeated(ref state, 1, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<SourceCodeInfo.Location>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            SourceCodeInfo.Location ISerializer<SourceCodeInfo.Location>.Read(ref ProtoReader.State state, SourceCodeInfo.Location value)
            {
                int num;
                if (value == null)
                {
                    value = new SourceCodeInfo.Location();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    int[] paths;
                    string str;
                    if (num == 1)
                    {
                        paths = value.Paths;
                        paths = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeSpecified, paths, null);
                        if (paths == null)
                        {
                            continue;
                        }
                        value.Paths = paths;
                        continue;
                    }
                    if (num == 2)
                    {
                        paths = value.Spans;
                        paths = RepeatedSerializer.CreateVector<int>().ReadRepeated(ref state, SerializerFeatures.WireTypeSpecified, paths, null);
                        if (paths == null)
                        {
                            continue;
                        }
                        value.Spans = paths;
                        continue;
                    }
                    if (num == 3)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.LeadingComments = str;
                        continue;
                    }
                    if (num == 4)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.TrailingComments = str;
                        continue;
                    }
                    if (num != 6)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    List<string> leadingDetachedComments = value.LeadingDetachedComments;
                    RepeatedSerializer.CreateList<string>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, leadingDetachedComments, null);
                }
                return value;
            }

            void ISerializer<SourceCodeInfo.Location>.Write(ref ProtoWriter.State state, SourceCodeInfo.Location value)
            {
                int[] numArray;
                string leadingComments;
                TypeModel.ThrowUnexpectedSubtype<SourceCodeInfo.Location>(value);
                int[] paths = value.Paths;
                if (paths == null)
                {
                    int[] local1 = paths;
                }
                else
                {
                    numArray = paths;
                    RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 1, SerializerFeatures.WireTypeSpecified, numArray, null);
                }
                int[] spans = value.Spans;
                if (spans == null)
                {
                    int[] local2 = spans;
                }
                else
                {
                    numArray = spans;
                    RepeatedSerializer.CreateVector<int>().WriteRepeated(ref state, 2, SerializerFeatures.WireTypeSpecified, numArray, null);
                }
                if (value.ShouldSerializeLeadingComments())
                {
                    leadingComments = value.LeadingComments;
                    state.WriteString(3, leadingComments, null);
                }
                if (value.ShouldSerializeTrailingComments())
                {
                    leadingComments = value.TrailingComments;
                    state.WriteString(4, leadingComments, null);
                }
                List<string> leadingDetachedComments = value.LeadingDetachedComments;
                if (leadingDetachedComments == null)
                {
                    List<string> local3 = leadingDetachedComments;
                }
                else
                {
                    List<string> values = leadingDetachedComments;
                    RepeatedSerializer.CreateList<string>().WriteRepeated(ref state, 6, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, null);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<UninterpretedOption>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            UninterpretedOption ISerializer<UninterpretedOption>.Read(ref ProtoReader.State state, UninterpretedOption value)
            {
                int num;
                if (value == null)
                {
                    value = new UninterpretedOption();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    if (num == 2)
                    {
                        List<UninterpretedOption.NamePart> names = value.Names;
                        RepeatedSerializer.CreateList<UninterpretedOption.NamePart>().ReadRepeated(ref state, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, names, this);
                        continue;
                    }
                    if (num == 3)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.IdentifierValue = str;
                        continue;
                    }
                    if (num == 4)
                    {
                        ulong num2 = state.ReadUInt64();
                        value.PositiveIntValue = num2;
                        continue;
                    }
                    if (num == 5)
                    {
                        long num3 = state.ReadInt64();
                        value.NegativeIntValue = num3;
                        continue;
                    }
                    if (num == 6)
                    {
                        double num4 = state.ReadDouble();
                        value.DoubleValue = num4;
                        continue;
                    }
                    if (num == 7)
                    {
                        byte[] stringValue = value.StringValue;
                        stringValue = state.AppendBytes(stringValue);
                        if (stringValue == null)
                        {
                            continue;
                        }
                        value.StringValue = stringValue;
                        continue;
                    }
                    if (num != 8)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    str = state.ReadString(null);
                    if (str != null)
                    {
                        value.AggregateValue = str;
                    }
                }
                return value;
            }

            void ISerializer<UninterpretedOption>.Write(ref ProtoWriter.State state, UninterpretedOption value)
            {
                string identifierValue;
                TypeModel.ThrowUnexpectedSubtype<UninterpretedOption>(value);
                List<UninterpretedOption.NamePart> names = value.Names;
                if (names == null)
                {
                    List<UninterpretedOption.NamePart> local1 = names;
                }
                else
                {
                    List<UninterpretedOption.NamePart> values = names;
                    RepeatedSerializer.CreateList<UninterpretedOption.NamePart>().WriteRepeated(ref state, 2, SerializerFeatures.OptionPackedDisabled | SerializerFeatures.WireTypeString, values, this);
                }
                if (value.ShouldSerializeIdentifierValue())
                {
                    identifierValue = value.IdentifierValue;
                    state.WriteString(3, identifierValue, null);
                }
                if (value.ShouldSerializePositiveIntValue())
                {
                    state.WriteFieldHeader(4, WireType.Variant);
                    ulong positiveIntValue = value.PositiveIntValue;
                    state.WriteUInt64(positiveIntValue);
                }
                if (value.ShouldSerializeNegativeIntValue())
                {
                    state.WriteFieldHeader(5, WireType.Variant);
                    long negativeIntValue = value.NegativeIntValue;
                    state.WriteInt64(negativeIntValue);
                }
                if (value.ShouldSerializeDoubleValue())
                {
                    state.WriteFieldHeader(6, WireType.Fixed64);
                    double doubleValue = value.DoubleValue;
                    state.WriteDouble(doubleValue);
                }
                if (value.ShouldSerializeStringValue())
                {
                    byte[] stringValue = value.StringValue;
                    if (stringValue == null)
                    {
                        byte[] local2 = stringValue;
                    }
                    else
                    {
                        state.WriteFieldHeader(7, WireType.String);
                        byte[] data = stringValue;
                        state.WriteBytes(data);
                    }
                }
                if (value.ShouldSerializeAggregateValue())
                {
                    identifierValue = value.AggregateValue;
                    state.WriteString(8, identifierValue, null);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<UninterpretedOption.NamePart>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            UninterpretedOption.NamePart ISerializer<UninterpretedOption.NamePart>.Read(ref ProtoReader.State state, UninterpretedOption.NamePart value)
            {
                int num;
                if (value == null)
                {
                    value = new UninterpretedOption.NamePart();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.name_part = str;
                        continue;
                    }
                    if (num != 2)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    bool flag = state.ReadBoolean();
                    value.IsExtension = flag;
                }
                return value;
            }

            void ISerializer<UninterpretedOption.NamePart>.Write(ref ProtoWriter.State state, UninterpretedOption.NamePart value)
            {
                TypeModel.ThrowUnexpectedSubtype<UninterpretedOption.NamePart>(value);
                string str = value.name_part;
                state.WriteString(1, str, null);
                state.WriteFieldHeader(2, WireType.Variant);
                bool isExtension = value.IsExtension;
                state.WriteBoolean(isExtension);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenEnumOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenEnumOptions ISerializer<ProtogenEnumOptions>.Read(ref ProtoReader.State state, ProtogenEnumOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenEnumOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    switch(num)
                    {
                        case 1:
                            string str = state.ReadString(null);
                            if (str != null)
                            {
                                value.Name = str;
                            }
                            break;
                        case 2:
                            Access access = (Access)state.ReadInt32();
                            value.Access = access;
                            break;
                        case 3:
                            str = state.ReadString(null);
                            if (str != null)
                            {
                                value.Namespace = str;
                            }
                            break;
                        default:
                            state.AppendExtensionData(value);
                            break;
                    }
                }
                return value;
            }

            void ISerializer<ProtogenEnumOptions>.Write(ref ProtoWriter.State state, ProtogenEnumOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ProtogenEnumOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    string str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                Access access = value.Access;
                if (access != Access.Inherit)
                {
                    int num = (int)access;
                    state.WriteInt32Varint(2, num);
                }
                state.WriteString(3, value.Namespace);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenEnumValueOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenEnumValueOptions ISerializer<ProtogenEnumValueOptions>.Read(ref ProtoReader.State state, ProtogenEnumValueOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenEnumValueOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num != 1)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    string str = state.ReadString(null);
                    if (str != null)
                    {
                        value.Name = str;
                    }
                }
                return value;
            }

            void ISerializer<ProtogenEnumValueOptions>.Write(ref ProtoWriter.State state, ProtogenEnumValueOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ProtogenEnumValueOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    string str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenFieldOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenFieldOptions ISerializer<ProtogenFieldOptions>.Read(ref ProtoReader.State state, ProtogenFieldOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenFieldOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    bool flag;
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        Access access = (Access)state.ReadInt32();
                        value.Access = access;
                        continue;
                    }
                    if (num == 3)
                    {
                        flag = state.ReadBoolean();
                        value.AsReference = flag;
                        continue;
                    }
                    if (num != 4)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    flag = state.ReadBoolean();
                    value.DynamicType = flag;
                }
                return value;
            }

            void ISerializer<ProtogenFieldOptions>.Write(ref ProtoWriter.State state, ProtogenFieldOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ProtogenFieldOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    string str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                Access access = value.Access;
                if (access != Access.Inherit)
                {
                    int num = (int)access;
                    state.WriteInt32Varint(2, num);
                }
                bool asReference = value.AsReference;
                if (asReference)
                {
                    state.WriteFieldHeader(3, WireType.Variant);
                    state.WriteBoolean(asReference);
                }
                asReference = value.DynamicType;
                if (asReference)
                {
                    state.WriteFieldHeader(4, WireType.Variant);
                    state.WriteBoolean(asReference);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenFileOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenFileOptions ISerializer<ProtogenFileOptions>.Read(ref ProtoReader.State state, ProtogenFileOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenFileOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    bool flag;
                    if (num == 1)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Namespace = str;
                        continue;
                    }
                    if (num == 2)
                    {
                        Access access = (Access)state.ReadInt32();
                        value.Access = access;
                        continue;
                    }
                    if (num == 3)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.ExtensionTypeName = str;
                        continue;
                    }
                    if (num == 4)
                    {
                        str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.CSharpLanguageVersion = str;
                        continue;
                    }
                    if (num == 5)
                    {
                        flag = state.ReadBoolean();
                        value.EmitRequiredDefaults = flag;
                        continue;
                    }
                    if (num == 6)
                    {
                        flag = state.ReadBoolean();
                        value.EmitOneOfEnum = flag;
                        continue;
                    }
                    if (num != 7)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    str = state.ReadString(null);
                    if (str != null)
                    {
                        value.VisualBasicLanguageVersion = str;
                    }
                }
                return value;
            }

            void ISerializer<ProtogenFileOptions>.Write(ref ProtoWriter.State state, ProtogenFileOptions value)
            {
                string str;
                TypeModel.ThrowUnexpectedSubtype<ProtogenFileOptions>(value);
                string text1 = value.Namespace;
                if (text1 == null)
                {
                    string local1 = text1;
                }
                else
                {
                    str = text1;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                Access access = value.Access;
                if (access != Access.Inherit)
                {
                    int num = (int)access;
                    state.WriteInt32Varint(2, num);
                }
                string extensionTypeName = value.ExtensionTypeName;
                if (extensionTypeName == null)
                {
                    string local2 = extensionTypeName;
                }
                else
                {
                    str = extensionTypeName;
                    if (str != "")
                    {
                        state.WriteString(3, str, null);
                    }
                }
                string cSharpLanguageVersion = value.CSharpLanguageVersion;
                if (cSharpLanguageVersion == null)
                {
                    string local3 = cSharpLanguageVersion;
                }
                else
                {
                    str = cSharpLanguageVersion;
                    if (str != "")
                    {
                        state.WriteString(4, str, null);
                    }
                }
                bool emitRequiredDefaults = value.EmitRequiredDefaults;
                if (emitRequiredDefaults)
                {
                    state.WriteFieldHeader(5, WireType.Variant);
                    state.WriteBoolean(emitRequiredDefaults);
                }
                emitRequiredDefaults = value.EmitOneOfEnum;
                if (emitRequiredDefaults)
                {
                    state.WriteFieldHeader(6, WireType.Variant);
                    state.WriteBoolean(emitRequiredDefaults);
                }
                string visualBasicLanguageVersion = value.VisualBasicLanguageVersion;
                if (visualBasicLanguageVersion == null)
                {
                    string local4 = visualBasicLanguageVersion;
                }
                else
                {
                    str = visualBasicLanguageVersion;
                    if (str != "")
                    {
                        state.WriteString(7, str, null);
                    }
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenMessageOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenMessageOptions ISerializer<ProtogenMessageOptions>.Read(ref ProtoReader.State state, ProtogenMessageOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenMessageOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    string str;
                    switch (num)
                    {
                        case 1:
                            str = state.ReadString(null);
                            if (str == null)
                            {
                                continue;
                            }
                            value.Name = str;
                            break;
                        case 2:
                            Access access = (Access)state.ReadInt32();
                            value.Access = access;
                            break;
                        case 3:
                            str = state.ReadString(null);
                            if (str != null)
                            {
                                value.ExtensionTypeName = str;
                            }
                            break;
                        case 4:
                            str = state.ReadString(null);
                            if (str != null)
                            {
                                value.Namespace = str;
                            }
                            break;
                        default:
                            state.AppendExtensionData(value);
                            break;
                    }
                }
                return value;
            }

            void ISerializer<ProtogenMessageOptions>.Write(ref ProtoWriter.State state, ProtogenMessageOptions value)
            {
                string str;
                TypeModel.ThrowUnexpectedSubtype<ProtogenMessageOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                Access access = value.Access;
                if (access != Access.Inherit)
                {
                    int num = (int)access;
                    state.WriteInt32Varint(2, num);
                }
                string extensionTypeName = value.ExtensionTypeName;
                if (extensionTypeName == null)
                {
                    string local2 = extensionTypeName;
                }
                else
                {
                    str = extensionTypeName;
                    if (str != "")
                    {
                        state.WriteString(3, str, null);
                    }
                }
                state.WriteString(4, value.Namespace);
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenMethodOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenMethodOptions ISerializer<ProtogenMethodOptions>.Read(ref ProtoReader.State state, ProtogenMethodOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenMethodOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num != 1)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    string str = state.ReadString(null);
                    if (str != null)
                    {
                        value.Name = str;
                    }
                }
                return value;
            }

            void ISerializer<ProtogenMethodOptions>.Write(ref ProtoWriter.State state, ProtogenMethodOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ProtogenMethodOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    string str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenOneofOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenOneofOptions ISerializer<ProtogenOneofOptions>.Read(ref ProtoReader.State state, ProtogenOneofOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenOneofOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num != 2)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    bool flag = state.ReadBoolean();
                    value.IsSubType = flag;
                }
                return value;
            }

            void ISerializer<ProtogenOneofOptions>.Write(ref ProtoWriter.State state, ProtogenOneofOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ProtogenOneofOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    string str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                bool isSubType = value.IsSubType;
                if (isSubType)
                {
                    state.WriteFieldHeader(2, WireType.Variant);
                    state.WriteBoolean(isSubType);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            SerializerFeatures ISerializer<ProtogenServiceOptions>.Features =>
                (SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString);

            ProtogenServiceOptions ISerializer<ProtogenServiceOptions>.Read(ref ProtoReader.State state, ProtogenServiceOptions value)
            {
                int num;
                if (value == null)
                {
                    value = new ProtogenServiceOptions();
                }
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    if (num == 1)
                    {
                        string str = state.ReadString(null);
                        if (str == null)
                        {
                            continue;
                        }
                        value.Name = str;
                        continue;
                    }
                    if (num != 2)
                    {
                        state.AppendExtensionData(value);
                        continue;
                    }
                    Access access = (Access)state.ReadInt32();
                    value.Access = access;
                }
                return value;
            }

            void ISerializer<ProtogenServiceOptions>.Write(ref ProtoWriter.State state, ProtogenServiceOptions value)
            {
                TypeModel.ThrowUnexpectedSubtype<ProtogenServiceOptions>(value);
                string name = value.Name;
                if (name == null)
                {
                    string local1 = name;
                }
                else
                {
                    string str = name;
                    if (str != "")
                    {
                        state.WriteString(1, str, null);
                    }
                }
                Access access = value.Access;
                if (access != Access.Inherit)
                {
                    int num = (int)access;
                    state.WriteInt32Varint(2, num);
                }
                IExtensible instance = value;
                state.AppendExtensionData(instance);
            }

            ISerializer<Access> ISerializerProxy<Access>.Serializer =>
                EnumSerializer.CreateInt32<Access>();

            ISerializer<Access?> ISerializerProxy<Access?>.Serializer =>
                EnumSerializer.CreateInt32<Access>();
        }
    }
}