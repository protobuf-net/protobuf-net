using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        /// <summary>
        /// Gets the default state associated with this writer
        /// </summary>
        protected internal abstract State DefaultState();

#if FEAT_COMPILER
        internal static readonly Type ByRefStateType = typeof(State).MakeByRefType();

        internal static MethodInfo GetStaticMethod(string name) =>
            MethodWrapper<ProtoWriter>.GetStaticMethod(name);
        internal static MethodInfo GetStaticMethod<T>(string name) =>
            MethodWrapper<T>.GetStaticMethod(name);
        private static class MethodWrapper<T>
        {
            private static readonly Dictionary<string, MethodInfo> _staticWriteMethods;

            public static MethodInfo GetStaticMethod(string name) => _staticWriteMethods[name];

            static MethodWrapper()
            {
                _staticWriteMethods = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var method in typeof(T)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.IsDefined(typeof(ObsoleteAttribute), true)) continue;
                    var args = method.GetParameters();
                    if (args == null || args.Length == 0) continue;

                    if(typeof(T) == typeof(ProtoWriter))
                    {
                        if (method.Name == nameof(ProtoWriter.Create)) continue; // ignore all of these
                        if (method.Name == nameof(ProtoWriter.WriteBytes)
                            && (args.Length == 5
                            || (args.Length != 0 && args[0].ParameterType == typeof(System.Buffers.ReadOnlySequence<byte>))
                        ))
                        {   // special omissions
                            continue;
                        }
                    }

                    bool haveState = false;
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].ParameterType == ByRefStateType)
                        {
                            haveState = true;
                            break;
                        }
                    }
                    if (!haveState) continue;
                    if (_staticWriteMethods.ContainsKey(method.Name))
                        throw new InvalidOperationException($"Ambiguous method: '{method.DeclaringType.Name}.{method.Name}'");
                    _staticWriteMethods.Add(method.Name, method);
                }
            }
        }
#endif
        /// <summary>
        /// Writer state
        /// </summary>
        public ref struct State
        {
            internal bool IsActive => !_span.IsEmpty;

            private Span<byte> _span;
            private Memory<byte> _memory;

            internal Span<byte> Remaining => _span.Slice(OffsetInCurrent);

            internal int RemainingInCurrent { get; private set; }
            internal int OffsetInCurrent { get; private set; }

            internal void Init(Memory<byte> memory)
            {
                _memory = memory;
                _span = memory.Span;
                RemainingInCurrent = _span.Length;
            }
            internal int Flush()
            {
                int val = OffsetInCurrent;
                this = default;
                return val;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void WriteFixed32(uint value)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(Remaining, value);
                OffsetInCurrent += 4;
                RemainingInCurrent -= 4;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Advance(int bytes)
            {
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
            }

            internal void WriteBytes(ReadOnlySpan<byte> span)
            {
                span.CopyTo(Remaining);
                OffsetInCurrent += span.Length;
                RemainingInCurrent -= span.Length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void WriteFixed64(ulong value)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(Remaining, value);
                OffsetInCurrent += 8;
                RemainingInCurrent -= 8;
            }

            internal void WriteString(string value)
            {
                int bytes;
#if PLAT_SPAN_OVERLOADS
                bytes = UTF8.GetBytes(value.AsSpan(), Remaining);
#else
                unsafe
                {
                    fixed (char* cPtr = value)
                    {
                        fixed (byte* bPtr = &MemoryMarshal.GetReference(_span))
                        {
                            bytes = UTF8.GetBytes(cPtr, value.Length,
                                bPtr + OffsetInCurrent, RemainingInCurrent);
                        }
                    }
                }
#endif
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
            }

            internal int WriteVarint64(ulong value)
            {
                int count = 0;
                var span = _span;
                var index = OffsetInCurrent;
                do
                {
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    count++;
                } while ((value >>= 7) != 0);
                span[index - 1] &= 0x7F;

                OffsetInCurrent += count;
                RemainingInCurrent -= count;
                return count;
            }

            internal int ReadFrom(Stream source)
            {
                int bytes;
                if (MemoryMarshal.TryGetArray<byte>(_memory, out var segment))
                {
                    bytes = source.Read(segment.Array, segment.Offset + OffsetInCurrent, RemainingInCurrent);
                }
                else
                {
#if PLAT_SPAN_OVERLOADS
                    bytes = source.Read(Remaining);
#else
                    var arr = System.Buffers.ArrayPool<byte>.Shared.Rent(RemainingInCurrent);
                    bytes = source.Read(arr, 0, RemainingInCurrent);
                    if (bytes > 0) new Span<byte>(arr, 0, bytes).CopyTo(Remaining);
                    System.Buffers.ArrayPool<byte>.Shared.Return(arr);
#endif
                }
                if (bytes > 0)
                {
                    OffsetInCurrent += bytes;
                    RemainingInCurrent -= bytes;
                }
                return bytes;
            }

            internal int WriteVarint32(uint value)
            {
                int count = 0;
                var span = _span;
                var index = OffsetInCurrent;
                do
                {
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    count++;
                } while ((value >>= 7) != 0);
                span[index - 1] &= 0x7F;

                OffsetInCurrent += count;
                RemainingInCurrent -= count;
                return count;
            }
        }
    }
}
