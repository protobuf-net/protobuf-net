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
        public object Visit(Stream stream, FileDescriptorProto file, string rootMessageType)
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
                Current = OnBeginMessage(null, descriptor);
                Visit(ref reader, descriptor);
                OnEndMessage(null, descriptor);
                Flush();
                return Current;
            }
            finally
            {
                reader.Dispose();
            }
        }

        public int Index { get; private set; }
        public object Current { get; private set; }
        public object Parent { get; private set; }
        private void Visit(ref ProtoReader.State reader, DescriptorProto descriptor)
        {
            // IMPORTANT TODO: no attempt made here to handle "repeated" yet, which is a huge omission;
            // in particular, we need to support "packed primitives" (but not packed message kinds)
            int fieldNumber, oldIndex = Index;
            object oldCurrent = Current, oldParent = Parent;
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                Index = -1;
                var field = descriptor.Fields.SingleOrDefault(x => x.Number == fieldNumber);
                if (field is null)
                {
                    OnUnkownField(ref reader);
                    continue;
                }
                bool isRepeated = field.label == FieldDescriptorProto.Label.LabelRepeated;
                if (isRepeated)
                {
                    Parent = Current;
                    Current = OnBeginRepeated(field);
                }
                do
                {
                    if (isRepeated)
                    {
                        Index++;
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
                        case FieldDescriptorProto.Type.TypeGroup: // this code is *designed* for TypeMessage, but should work for TypeGroup too?
                            if (_knownTypes.TryGetValue(field.TypeName, out var inner) && inner is DescriptorProto messageType)
                            {
                                var tok = reader.StartSubItem();
                                Parent = Current;
                                Current = OnBeginMessage(field, messageType);
                                Visit(ref reader, messageType);
                                OnEndMessage(field, messageType);
                                Current = Parent; // deliberately *not* oldCurrent, because this could be the list
                                Parent = oldParent;
                                reader.EndSubItem(tok);
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to locate sub-message kind: " + field.TypeName);
                            }
                            break;
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
                                OnField(field, found, value);
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to locate enum kind: " + field.TypeName);
                            }
                            break;
                        default: // unexpected things
                            throw new InvalidOperationException($"unexpected proto type: {field.type}");
                    }
                } while (isRepeated && reader.TryReadFieldHeader(fieldNumber));

                if (isRepeated)
                {
                    Index++; // for use as a final count
                    OnEndRepeated(field);
                }
                // reset after every field - can get munged by repeated etc
                Current = oldCurrent;
                Parent = oldParent;
            }
            Index = oldIndex;
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
        protected virtual void OnField(FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value)
        {
            if (@enum is null) OnFieldFallback(field, value.ToString(FormatProvider));
            else OnFieldFallback(field, @enum.Name);
        }
        protected virtual object OnBeginMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            Depth++;
            return null;
        }
        protected virtual void OnEndMessage(FieldDescriptorProto field, DescriptorProto message) => Depth--;

        protected virtual void OnUnkownField(ref ProtoReader.State reader) => reader.SkipField();

        protected virtual object OnBeginRepeated(FieldDescriptorProto field)
        {
            Depth++;
            return null;
        }

        protected virtual void OnEndRepeated(FieldDescriptorProto field) => Depth--;

        protected virtual void Flush() { }

        public int Depth { get; private set; } = -1;
        public virtual void Dispose() { }
    }
}
