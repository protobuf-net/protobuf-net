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
                switch (field.type)
                {
                    case FieldDescriptorProto.Type.TypeBool:
                        OnField(field, reader.ReadBoolean());
                        break;
                    case FieldDescriptorProto.Type.TypeSfixed32:
                    case FieldDescriptorProto.Type.TypeInt32:
                        OnField(field, reader.ReadInt32());
                        break;
                    case FieldDescriptorProto.Type.TypeFixed32:
                        OnField(field, reader.ReadUInt32());
                        break;
                    case FieldDescriptorProto.Type.TypeSint32:
                        reader.Hint(WireType.SignedVarint);
                        OnField(field, reader.ReadInt32());
                        break;
                    case FieldDescriptorProto.Type.TypeDouble:
                        OnField(field, reader.ReadDouble());
                        break;
                    case FieldDescriptorProto.Type.TypeFloat:
                        OnField(field, reader.ReadSingle());
                        break;
                    case FieldDescriptorProto.Type.TypeString:
                        OnField(field, reader.ReadString());
                        break;
                    case FieldDescriptorProto.Type.TypeSfixed64:
                    case FieldDescriptorProto.Type.TypeInt64:
                        OnField(field, reader.ReadInt64());
                        break;
                    case FieldDescriptorProto.Type.TypeSint64:
                        reader.Hint(WireType.SignedVarint);
                        OnField(field, reader.ReadInt64());
                        break;
                    case FieldDescriptorProto.Type.TypeUint32:
                        OnField(field, reader.ReadUInt32());
                        break;
                    case FieldDescriptorProto.Type.TypeFixed64:
                    case FieldDescriptorProto.Type.TypeUint64:
                        OnField(field, reader.ReadUInt64());
                        break;
                    case FieldDescriptorProto.Type.TypeBytes:
                        OnField(field, reader.AppendBytes(null));
                        break;
                    case FieldDescriptorProto.Type.TypeMessage:
                        if (_knownTypes.TryGetValue(field.TypeName, out var inner) && inner is DescriptorProto innerDescriptor)
                        {
                            var tok = reader.StartSubItem();
                            OnBeginMessage(field, innerDescriptor);
                            Visit(ref reader, innerDescriptor);
                            OnEndMessage(field, innerDescriptor);
                            reader.EndSubItem(tok);
                        }
                        else
                        {
                            throw new InvalidOperationException("Unable to locate sub-message kind: " + field.TypeName);
                        }
                        break;
                    // things we don't handle yet
                    case FieldDescriptorProto.Type.TypeEnum:
                        throw new NotImplementedException($"proto type not handled yet: {field.type}");
                    // (TODO: read as a varint (32-bit), and resolve the name from the enum descriptor if possible, but note
                    // that unknown values should be handled and presented as integers)
                    // things we will probably never handle
                    case FieldDescriptorProto.Type.TypeGroup:
                        throw new NotSupportedException("groups are not supported"); // you will probably never need this
                    // unexpected things
                    default: // 
                        throw new InvalidOperationException($"unexpected proto type: {field.type}");
                }
            }
        }

        public IFormatProvider FormatProvider { get; set; } = CultureInfo.InvariantCulture;
        protected abstract void OnFieldFallback(FieldDescriptorProto field, string value); // fallback to allow simple shared handling

        protected virtual void OnField(FieldDescriptorProto field, bool value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, int value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, uint value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, long value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, ulong value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, float value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, double value) => OnFieldFallback(field, value.ToString(FormatProvider));
        protected virtual void OnField(FieldDescriptorProto field, string value) => OnFieldFallback(field, value);
        protected virtual void OnField(FieldDescriptorProto field, byte[] value) => OnFieldFallback(field, BitConverter.ToString(value));
        protected virtual void OnBeginMessage(FieldDescriptorProto field, DescriptorProto message) => Depth++;
        protected virtual void OnEndMessage(FieldDescriptorProto field, DescriptorProto message) => Depth--;
        protected virtual void OnUnkownField(ref ProtoReader.State reader) => reader.SkipField();

        protected virtual void Flush() { }

        public int Depth { get; private set; }
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

        protected override void OnFieldFallback(FieldDescriptorProto field, string value)
            => WriteLine($"{field.Number}: {field.Name}={value} ({field.type})");

        protected override void OnBeginMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            WriteLine($"{field.Number}: {field.Name}={{");
            base.OnBeginMessage(field, message);
        }
        protected override void OnEndMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            base.OnEndMessage(field, message);
            WriteLine($"}} // {field.Name}");
        }
    }
}
