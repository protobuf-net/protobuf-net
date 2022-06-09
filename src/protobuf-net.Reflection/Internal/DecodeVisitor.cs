using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ProtoBuf.Internal
{
    internal abstract class DecodeVisitor : IDisposable
    {
        private readonly Dictionary<string, object> _knownTypes = new Dictionary<string, object>();
        public void Visit(Stream stream, FileDescriptorProto file, string rootMessageType)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (file is null) throw new ArgumentNullException(nameof(file));

            // build an index over the known types; note that this uses .-rooted syntax, so make sure
            // that our input matches that if needed
            CommonCodeGenerator.BuildTypeIndex(file, _knownTypes);
            rootMessageType = (rootMessageType ?? "").Trim();
            if (rootMessageType.Length > 0 && rootMessageType[0] != '.') rootMessageType = "." + rootMessageType;

            if (_knownTypes.TryGetValue(rootMessageType, out var found) && found is DescriptorProto descriptor)
            { } // fine!
            else
            {
                throw new InvalidOperationException($"Unable to resolve root message kind '{rootMessageType}' from {file.Name}");
            }
            var reader = ProtoReader.State.Create(stream, null);
            try
            {
                Visit(ref reader, descriptor);
                Flush();
            }
            finally
            {
                reader.Dispose();
            }
        }

        private void Visit(ref ProtoReader.State reader, DescriptorProto descriptor)
        {
            // IMPORTANT TODO: no attempt made here to handle "repeated" yet, which is a huge omission;
            // in particular, we need to support "packed primitives" (but not packed message kinds)
            int fieldNumber;
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                var field = descriptor.Fields.SingleOrDefault(x => x.Number == fieldNumber);
                if (field is null)
                {
                    OnUnkownField(ref reader);
                    continue;
                }
                int repeatedIndex = field.label == FieldDescriptorProto.Label.LabelRepeated ? 0 : -1;
                if (repeatedIndex >= 0)
                {
                    OnBeginRepeated(field);
                }
                do
                {
                    switch (field.type)
                    {
                        case FieldDescriptorProto.Type.TypeBool:
                            OnField(field, reader.ReadBoolean(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeSfixed32:
                        case FieldDescriptorProto.Type.TypeInt32:
                            OnField(field, reader.ReadInt32(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeFixed32:
                            OnField(field, reader.ReadUInt32(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeSint32:
                            reader.Hint(WireType.SignedVarint);
                            OnField(field, reader.ReadInt32(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeDouble:
                            OnField(field, reader.ReadDouble(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeFloat:
                            OnField(field, reader.ReadSingle(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeString:
                            OnField(field, reader.ReadString(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeSfixed64:
                        case FieldDescriptorProto.Type.TypeInt64:
                            OnField(field, reader.ReadInt64(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeSint64:
                            reader.Hint(WireType.SignedVarint);
                            OnField(field, reader.ReadInt64(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeUint32:
                            OnField(field, reader.ReadUInt32(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeFixed64:
                        case FieldDescriptorProto.Type.TypeUint64:
                            OnField(field, reader.ReadUInt64(), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeBytes:
                            OnField(field, reader.AppendBytes(null), repeatedIndex);
                            break;
                        case FieldDescriptorProto.Type.TypeMessage:
                            if (_knownTypes.TryGetValue(field.TypeName, out var inner) && inner is DescriptorProto messageType)
                            {
                                var tok = reader.StartSubItem();
                                OnBeginMessage(field, messageType, repeatedIndex);
                                Visit(ref reader, messageType);
                                OnEndMessage(field, messageType, repeatedIndex);
                                reader.EndSubItem(tok);
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to locate sub-message kind: " + field.TypeName);
                            }
                            break;
                        // things we don't handle yet
                        case FieldDescriptorProto.Type.TypeEnum:
                            if (_knownTypes.TryGetValue(field.TypeName, out inner) && inner is EnumDescriptorProto enumDescriptor)
                            {
                                var value = reader.ReadInt32();
                                EnumValueDescriptorProto found = null;
                                foreach (var defined in enumDescriptor.Values)
                                {
                                    if (defined.Number == value)
                                    {
                                        found = defined;
                                        break;
                                    }
                                }
                                OnField(field, found, value, repeatedIndex);
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to locate enum kind: " + field.TypeName);
                            }
                            break;
                        // things we will probably never handle
                        case FieldDescriptorProto.Type.TypeGroup:
                            throw new NotSupportedException("groups are not supported"); // you will probably never need this
                                                                                         // unexpected things
                        default: // 
                            throw new InvalidOperationException($"unexpected proto type: {field.type}");
                    }
                    if (repeatedIndex >= 0) repeatedIndex++;
                } while (repeatedIndex >= 0 && reader.TryReadFieldHeader(fieldNumber));

                if (repeatedIndex >= 0)
                {
                    OnEndRepeated(field, repeatedIndex);
                }
            }
        }

        public IFormatProvider FormatProvider { get; set; } = CultureInfo.InvariantCulture;
        protected abstract void OnFieldFallback(FieldDescriptorProto field, string value, int index); // fallback to allow simple shared handling

        protected virtual void OnField(FieldDescriptorProto field, bool value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, int value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, uint value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, long value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, ulong value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, float value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, double value, int index) => OnFieldFallback(field, value.ToString(FormatProvider), index);
        protected virtual void OnField(FieldDescriptorProto field, string value, int index) => OnFieldFallback(field, value, index);
        protected virtual void OnField(FieldDescriptorProto field, byte[] value, int index) => OnFieldFallback(field, BitConverter.ToString(value), index);
        private void OnField(FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value, int index)
        {
            if (@enum is null) OnFieldFallback(field, value.ToString(FormatProvider), index);
            else OnFieldFallback(field, @enum.Name, index);
        }
        protected virtual void OnBeginMessage(FieldDescriptorProto field, DescriptorProto message, int index) => Depth++;
        protected virtual void OnEndMessage(FieldDescriptorProto field, DescriptorProto message, int index) => Depth--;
        protected virtual void OnUnkownField(ref ProtoReader.State reader) => reader.SkipField();

        protected virtual void OnBeginRepeated(FieldDescriptorProto field) => Depth++;

        protected virtual void OnEndRepeated(FieldDescriptorProto field, int count) => Depth--;

        protected virtual void Flush() { }

        public int Depth { get; protected set; }
        public virtual void Dispose() { }
    }

    internal sealed class TextDecodeVisitor : DecodeVisitor
    {
        public TextWriter Output { get; }
        public TextDecodeVisitor(TextWriter output)
            => Output = output ?? throw new ArgumentNullException(nameof(output));
        public string Indent { get; set; } = " ";

        protected override void Flush() => Output.Flush();

        private void WriteLine(string message)
        {
            for (int i = 0; i < Depth; i++)
                Output.Write(Indent);
            Output.WriteLine(message);
        }
        protected override void OnUnkownField(ref ProtoReader.State reader)
        {
            WriteLine($"{reader.FieldNumber}: (unknown, {reader.WireType})");
            base.OnUnkownField(ref reader); // skip the value
        }

        protected override void OnBeginRepeated(FieldDescriptorProto field)
        {
            WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}=[ ({field.type})");
            base.OnBeginRepeated(field);
        }
        protected override void OnEndRepeated(FieldDescriptorProto field, int count)
        {
            base.OnEndRepeated(field, count);
            WriteLine($"] // {field.Name}, count: {count.ToString(FormatProvider)}");
        }
        protected override void OnFieldFallback(FieldDescriptorProto field, string value, int index)
        {
            if (index < 0)
            {
                WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={value} ({field.type})");
            }
            else
            {
                WriteLine($"#{index.ToString(FormatProvider)}={value}");
            }
        }

        protected override void OnBeginMessage(FieldDescriptorProto field, DescriptorProto message, int index)
        {
            if (index < 0)
            {
                WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={{");
            }
            else
            {
                WriteLine($"#{index.ToString(FormatProvider)}={{");
            }
            base.OnBeginMessage(field, message, index);
        }
        protected override void OnEndMessage(FieldDescriptorProto field, DescriptorProto message, int index)
        {
            base.OnEndMessage(field, message, index);
            WriteLine($"}} // {field.Name}");
        }
    }
}
