using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

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
                var ctx = new VisitContext(null, null, descriptor);
                var obj = OnBeginMessage(in ctx, null);
                ctx = ctx.StepIn(obj);

                VisitMessageFields(ctx, ref reader);
                OnEndMessage(in ctx, null);
                Flush();
                return ctx.Current;
            }
            finally
            {
                reader.Dispose();
            }
        }

        protected readonly struct VisitContext
        {
            private readonly DiscriminatedUnion64Object _mapKey;
            private readonly DescriptorProto _messageType;
            private readonly object _current, _parent;
            private readonly int _index;
            public object Current => _current;
            public object Parent => _parent;
            public int Index => _index;
            public DescriptorProto MessageType => _messageType;
            public VisitContext(object current, object parent, DescriptorProto message, int index = -1)
            {
                _current = current;
                _parent = parent;
                _index = index;
                _mapKey = default;
                _messageType = message;
            }
            private VisitContext(in VisitContext value, DiscriminatedUnion64Object mapKey)
            {
                this = value;
                _mapKey = mapKey;
            }

            internal VisitContext StepIn(object obj) => new VisitContext(obj, this._current, this._messageType);

            internal void UnsafeIncrIndex() => Unsafe.AsRef(in _index)++;

            internal VisitContext WithMessageType(DescriptorProto messageType)
            {
                var clone = this;
                Unsafe.AsRef(in clone._messageType) = messageType;
                return clone;
            }

            public MapKeyKind MapKeyKind => (MapKeyKind)_mapKey.Discriminator;
            public int MapKeyInt32 => _mapKey.Int32;
            public long MapKeyInt64 => _mapKey.Int64;
            public uint MapKeyUInt32 => _mapKey.UInt32;
            public ulong MapKeyUInt64 => _mapKey.UInt64;
            public string MapKeyString => (string)_mapKey.Object;

            public object MapKeyValue => (MapKeyKind)_mapKey.Discriminator switch
            {
                MapKeyKind.Int32 => _mapKey.Int32,
                MapKeyKind.Int64 => _mapKey.Int64,
                MapKeyKind.UInt32 => MapKeyUInt32,
                MapKeyKind.UInt64 => MapKeyUInt64,
                MapKeyKind.String => MapKeyString,
                _ => null,
            };

            internal VisitContext WithMapKey<TKey>(TKey key)
            {
                if (typeof(TKey) == typeof(int))
                {
                    return new VisitContext(in this, new DiscriminatedUnion64Object((int)MapKeyKind.Int32, Unsafe.As<TKey, int>(ref key)));
                }
                else if (typeof(TKey) == typeof(uint))
                {
                    return new VisitContext(in this, new DiscriminatedUnion64Object((int)MapKeyKind.UInt32, Unsafe.As<TKey, uint>(ref key)));
                }
                else if (typeof(TKey) == typeof(long))
                {
                    return new VisitContext(in this, new DiscriminatedUnion64Object((int)MapKeyKind.Int64, Unsafe.As<TKey, long>(ref key)));
                }
                else if (typeof(TKey) == typeof(ulong))
                {
                    return new VisitContext(in this, new DiscriminatedUnion64Object((int)MapKeyKind.UInt64, Unsafe.As<TKey, ulong>(ref key)));
                }
                else if (typeof(TKey) == typeof(string))
                {
                    return new VisitContext(in this, new DiscriminatedUnion64Object((int)MapKeyKind.String, Unsafe.As<TKey, string>(ref key)));
                }
                return this;
            }
        }

        protected enum MapKeyKind
        {
            None,
            Int32,
            Int64,
            UInt32,
            UInt64,
            String,
        }

        private void VisitMessageFields(VisitContext ctx, ref ProtoReader.State reader)
        {
            int fieldNumber;
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                FieldDescriptorProto field = null;
                foreach (var test in ctx.MessageType.Fields)
                {
                    if (test.Number == fieldNumber)
                    {
                        if (field is not null) throw new InvalidOperationException($"Duplicate field: {fieldNumber}");
                        field = test;
                    }
                }
                if (field is null)
                {
                    OnUnkownField(in ctx, ref reader);
                    continue;
                }
                bool isRepeated = field.label == FieldDescriptorProto.Label.LabelRepeated;
                long packedEnd = -1;
                var packedWireType = reader.WireType;
                if (isRepeated && reader.WireType == WireType.String && FieldDescriptorProto.CanPack(field.type))
                {
                    var bytes = reader.ReadUInt32Varint(ProtoReader.Read32VarintMode.Unsigned);
                    packedEnd = reader.GetPosition() + bytes;
                    packedWireType = field.type switch
                    {
                        FieldDescriptorProto.Type.TypeBool => WireType.Varint,
                        FieldDescriptorProto.Type.TypeDouble => WireType.Fixed64,
                        FieldDescriptorProto.Type.TypeFloat => WireType.Fixed32,
                        FieldDescriptorProto.Type.TypeInt64 => WireType.Varint,
                        FieldDescriptorProto.Type.TypeUint64 => WireType.Varint,
                        FieldDescriptorProto.Type.TypeInt32 => WireType.Varint,
                        FieldDescriptorProto.Type.TypeFixed64 => WireType.Fixed64,
                        FieldDescriptorProto.Type.TypeFixed32 => WireType.Fixed32,
                        FieldDescriptorProto.Type.TypeUint32 => WireType.Varint,
                        FieldDescriptorProto.Type.TypeEnum => WireType.Varint,
                        FieldDescriptorProto.Type.TypeSfixed32 => WireType.Fixed32,
                        FieldDescriptorProto.Type.TypeSfixed64 => WireType.Fixed64,
                        FieldDescriptorProto.Type.TypeSint32 => WireType.Varint,
                        FieldDescriptorProto.Type.TypeSint64 => WireType.Varint,
                        _ => throw new NotImplementedException($"No packed wire type specified for: {field.type}"),
                    };
                }

                DescriptorProto messageType = null;
                switch (field.type)
                {
                    case FieldDescriptorProto.Type.TypeMessage:
                    case FieldDescriptorProto.Type.TypeGroup:
                        if (!string.IsNullOrEmpty(field.TypeName) && _knownTypes.TryGetValue(field.TypeName, out var inner)
                            && inner is DescriptorProto tmp)
                        {
                            if (isRepeated && tmp.Options?.MapEntry == true)
                            {
                                // pull maps out - needs some generic love
                                VisitMap(ctx, ref reader, field, tmp);
                                continue;
                            }
                            messageType = tmp;
                        }
                        else
                        {
                            throw new InvalidOperationException("Unable to locate sub-message kind: " + field.TypeName);
                        }
                        break;
                }

                VisitContext ctxSnapshot = ctx;
                if (isRepeated)
                {
                    var obj = OnBeginRepeated(in ctx, field);
                    ctx = ctx.StepIn(obj);
                }
                do
                {
                    if (isRepeated)
                    {
                        if (packedEnd >= 0) // data is packed
                        {
                            // packed could technically be zero bytes; rather than complicate the loop, we'll
                            // check for "end of packed data" at the start of each loop iteration, so we can
                            // exclude that zero case
                            var position = reader.GetPosition();
                            if (position >= packedEnd)
                            {
                                if (position == packedEnd) break; // exit loop; we've consumed all the packed data
                                throw new InvalidOperationException("Incorrectly read past packed data"); // this shouldn't happen
                            }
                            reader.WireType = packedWireType; // spoof the wire type
                        }
                        ctx.UnsafeIncrIndex();
                    }

                    switch (field.type)
                    {
                        case FieldDescriptorProto.Type.TypeBool:
                            OnField(in ctx, field, reader.ReadBoolean());
                            break;
                        case FieldDescriptorProto.Type.TypeSfixed32:
                        case FieldDescriptorProto.Type.TypeInt32:
                            OnField(in ctx, field, reader.ReadInt32());
                            break;
                        case FieldDescriptorProto.Type.TypeFixed32:
                            OnField(in ctx, field, reader.ReadUInt32());
                            break;
                        case FieldDescriptorProto.Type.TypeSint32:
                            reader.Hint(WireType.SignedVarint);
                            OnField(in ctx, field, reader.ReadInt32());
                            break;
                        case FieldDescriptorProto.Type.TypeDouble:
                            OnField(in ctx, field, reader.ReadDouble());
                            break;
                        case FieldDescriptorProto.Type.TypeFloat:
                            OnField(in ctx, field, reader.ReadSingle());
                            break;
                        case FieldDescriptorProto.Type.TypeString:
                            OnField(in ctx, field, reader.ReadString());
                            break;
                        case FieldDescriptorProto.Type.TypeSfixed64:
                        case FieldDescriptorProto.Type.TypeInt64:
                            OnField(in ctx, field, reader.ReadInt64());
                            break;
                        case FieldDescriptorProto.Type.TypeSint64:
                            reader.Hint(WireType.SignedVarint);
                            OnField(in ctx, field, reader.ReadInt64());
                            break;
                        case FieldDescriptorProto.Type.TypeUint32:
                            OnField(in ctx, field, reader.ReadUInt32());
                            break;
                        case FieldDescriptorProto.Type.TypeFixed64:
                        case FieldDescriptorProto.Type.TypeUint64:
                            OnField(in ctx, field, reader.ReadUInt64());
                            break;
                        case FieldDescriptorProto.Type.TypeBytes:
                            OnField(in ctx, field, reader.AppendBytes(null));
                            break;
                        case FieldDescriptorProto.Type.TypeMessage:
                        case FieldDescriptorProto.Type.TypeGroup: // this code is *designed* for TypeMessage, but should work for TypeGroup too?
                            // note: messageType has already been retreived
                            ReadMessage(ctx.WithMessageType(messageType), ref reader, field);
                            break;
                        case FieldDescriptorProto.Type.TypeEnum:
                            if (TryGetEnumType(field, out var enumDescriptor))
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
                                OnField(in ctx, field, found, value);
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to locate enum kind: " + field.TypeName);
                            }
                            break;
                        default: // unexpected things
                            throw new InvalidOperationException($"unexpected proto type: {field.type}");
                    }

                    // keep looping if "packed" (tests for exit at start of loop), or if we're still reading the same field number
                } while (isRepeated && (packedEnd >= 0 || reader.TryReadFieldHeader(fieldNumber)));

                if (isRepeated)
                {
                    ctx.UnsafeIncrIndex(); // for use as a final count
                    OnEndRepeated(in ctx, field);
                    ctx = ctxSnapshot; // step back out
                }
            }
        }

        private void VisitMap(VisitContext ctx, ref ProtoReader.State reader, FieldDescriptorProto mapField, DescriptorProto descriptor)
        {
            ctx = ctx.WithMessageType(descriptor);
            FieldDescriptorProto keyField = null, valueField = null;
            foreach (var field in descriptor.Fields)
            {
                switch (field.Number)
                {
                    case 1: keyField = field; break;
                    case 2: valueField = field; break;
                }
            }
            if (keyField is null || valueField is null) throw new InvalidOperationException($"Error processing map: {descriptor.Name}");
            switch (keyField.type)
            {
                case FieldDescriptorProto.Type.TypeInt32:
                case FieldDescriptorProto.Type.TypeSint32:
                case FieldDescriptorProto.Type.TypeSfixed32:
                    VisitMap<int>(in ctx, ref reader, static (in VisitContext ctx, ref ProtoReader.State reader) => reader.ReadInt32(), mapField, valueField);
                    break;
                case FieldDescriptorProto.Type.TypeInt64:
                case FieldDescriptorProto.Type.TypeSint64:
                case FieldDescriptorProto.Type.TypeSfixed64:
                    VisitMap<long>(in ctx, ref reader, static (in VisitContext ctx, ref ProtoReader.State reader) => reader.ReadInt64(), mapField, valueField);
                    break;
                case FieldDescriptorProto.Type.TypeUint32:
                case FieldDescriptorProto.Type.TypeFixed32:
                    VisitMap<uint>(in ctx, ref reader, static (in VisitContext ctx, ref ProtoReader.State reader) => reader.ReadUInt32(), mapField, valueField);
                    break;
                case FieldDescriptorProto.Type.TypeUint64:
                case FieldDescriptorProto.Type.TypeFixed64:
                    VisitMap<ulong>(in ctx, ref reader, static (in VisitContext ctx, ref ProtoReader.State reader) => reader.ReadUInt64(), mapField, valueField);
                    break;
                case FieldDescriptorProto.Type.TypeString:
                    VisitMap<string>(in ctx, ref reader, static (in VisitContext ctx, ref ProtoReader.State reader) => reader.ReadString(), mapField, valueField, "");
                    break;
                default:
                    throw new InvalidOperationException($"Invalid map key type: {keyField.type}");
            }
        }

        private delegate T Reader<T>(in VisitContext ctx, ref ProtoReader.State state);

        private void VisitMap<TKey>(in VisitContext ctx, ref ProtoReader.State reader, Reader<TKey> keyReader,
            FieldDescriptorProto mapField, FieldDescriptorProto valueField, TKey defaultKey = default)
        {
            switch (valueField.type)
            {
                case FieldDescriptorProto.Type.TypeInt32:
                case FieldDescriptorProto.Type.TypeSint32:
                case FieldDescriptorProto.Type.TypeSfixed32:
                    VisitMap<TKey, int>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadInt32(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeInt64:
                case FieldDescriptorProto.Type.TypeSint64:
                case FieldDescriptorProto.Type.TypeSfixed64:
                    VisitMap<TKey, long>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadInt64(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeUint32:
                case FieldDescriptorProto.Type.TypeFixed32:
                    VisitMap<TKey, uint>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadUInt32(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeUint64:
                case FieldDescriptorProto.Type.TypeFixed64:
                    VisitMap<TKey, ulong>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadUInt64(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeString:
                    VisitMap<TKey, string>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadString(), mapField, valueField, defaultKey, "");
                    break;
                case FieldDescriptorProto.Type.TypeDouble:
                    VisitMap<TKey, double>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadDouble(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeFloat:
                    VisitMap<TKey, float>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadSingle(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeBool:
                    VisitMap<TKey, bool>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.ReadBoolean(), mapField, valueField, defaultKey);
                    break;
                case FieldDescriptorProto.Type.TypeBytes:
                    VisitMap<TKey, byte[]>(ctx, ref reader, keyReader, static (in VisitContext _, ref ProtoReader.State reader) => reader.AppendBytes(null), mapField, valueField, defaultKey, Array.Empty<byte>());
                    break;
                case FieldDescriptorProto.Type.TypeGroup:
                case FieldDescriptorProto.Type.TypeMessage:
                    if (!string.IsNullOrEmpty(valueField.TypeName) && _knownTypes.TryGetValue(valueField.TypeName, out var inner)
                            && inner is DescriptorProto messageType)
                    {
                        var captureMapField = mapField;
                        VisitMap<TKey, object>(ctx.WithMessageType(messageType), ref reader, keyReader, (in VisitContext ctx, ref ProtoReader.State reader) => ReadMessage(ctx, ref reader, captureMapField), mapField, valueField, defaultKey);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unable to locate sub-message kind: " + valueField.TypeName);
                    }
                    break;
                case FieldDescriptorProto.Type.TypeEnum:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException($"Invalid map value type: {valueField.type}");
            }
        }

        private object ReadMessage(VisitContext ctx, ref ProtoReader.State reader, FieldDescriptorProto field)
        {
            var tok = reader.StartSubItem();
            var obj = OnBeginMessage(in ctx, field);
            ctx = ctx.StepIn(obj);
            VisitMessageFields(ctx, ref reader);
            OnEndMessage(in ctx, field);
            reader.EndSubItem(tok);
            return obj;
        }

        private void VisitMap<TKey, TValue>(VisitContext ctx, ref ProtoReader.State reader,
            Reader<TKey> keyReader,
            Reader<TValue> valueReader,
            FieldDescriptorProto mapField, FieldDescriptorProto valueField, TKey defaultKey, TValue defaultValue = default)
        {
            ctx = ctx.StepIn(OnBeginMap<TKey, TValue>(in ctx, mapField)).WithMapKey(defaultKey);
            ctx.UnsafeIncrIndex();
            do
            {
                var tok = reader.StartSubItem();
                int field;
                TKey key = defaultKey;
                TValue value = defaultValue;
                while ((field = reader.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            key = keyReader(in ctx, ref reader);
                            ctx = ctx.WithMapKey<TKey>(key); // this needs to be called before the value-read
                            break;
                        case 2:
                            value = valueReader(in ctx, ref reader);
                            break;
                        default:
                            reader.SkipField();
                            break;
                    }
                }
                reader.EndSubItem(tok);
                OnMapEntry<TKey, TValue>(in ctx, valueField.type, key, value);
                ctx.UnsafeIncrIndex();
            } while (reader.TryReadFieldHeader(mapField.Number));
            OnEndMap<TKey, TValue>(in ctx, mapField);
        }

        protected bool TryGetEnumType(FieldDescriptorProto field, out EnumDescriptorProto enumDescriptor)
        {
            if (_knownTypes.TryGetValue(field.TypeName, out var inner) && inner is EnumDescriptorProto tmp)
            {
                enumDescriptor = tmp;
                return true;
            }
            enumDescriptor = null;
            return false;
        }
        public IFormatProvider FormatProvider { get; set; } = CultureInfo.InvariantCulture;
        protected abstract void OnFieldFallback(in VisitContext ctx, FieldDescriptorProto field, string value); // fallback to allow simple shared handling

        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, bool value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, int value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, uint value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, long value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, ulong value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, float value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, double value) => OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, string value) => OnFieldFallback(in ctx, field, value);
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, byte[] value) => OnFieldFallback(in ctx, field, BitConverter.ToString(value));
        protected virtual void OnField(in VisitContext ctx, FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value)
        {
            if (@enum is null) OnFieldFallback(in ctx, field, value.ToString(FormatProvider));
            else OnFieldFallback(in ctx, field, @enum.Name);
        }
        protected virtual object OnBeginMessage(in VisitContext ctx, FieldDescriptorProto field)
        {
            Depth++;
            return null;
        }
        protected virtual void OnEndMessage(in VisitContext ctx, FieldDescriptorProto field) => Depth--;

        protected virtual void OnUnkownField(in VisitContext ctx, ref ProtoReader.State reader) => reader.SkipField();

        protected virtual object OnBeginRepeated(in VisitContext ctx, FieldDescriptorProto field)
        {
            Depth++;
            return null;
        }

        protected virtual void OnEndRepeated(in VisitContext ctx, FieldDescriptorProto field) => Depth--;

        protected virtual object OnBeginMap<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto field)
        {
            Depth++;
            return null;
        }

        protected virtual void OnMapEntry<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto.Type valueType, TKey key, TValue value) { }

        protected virtual void OnEndMap<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto field) => Depth--;

        protected virtual void Flush() { }

        public int Depth { get; private set; } = -1;
        public virtual void Dispose() { }
    }
}
