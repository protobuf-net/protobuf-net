﻿using Google.Protobuf.Reflection;
using System;
using System.Linq;

namespace ProtoBuf.Reflection
{
    public partial class CommonCodeGenerator
    {
        private protected static bool UseMemory(GeneratorContext ctx)
            => string.Equals(ctx.GetCustomOption("bytes"), "Memory", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Represents the union summary of a one-of declaration
        /// </summary>
        protected class OneOfStub
        {
            /// <summary>
            /// The underlying descriptor
            /// </summary>
            public OneofDescriptorProto OneOf { get; }

            /// <summary>
            /// The effective index of this stub
            /// </summary>
            public int Index { get; }

            internal OneOfStub(OneofDescriptorProto decl, int index)
            {
                OneOf = decl;
                Index = index;
            }
            internal int Count32 { get; private set; }
            internal int Count64 { get; private set; }
            internal int Count128 { get; private set; }
            internal int CountRef { get; private set; }
            internal int CountTotal => CountRef + Count32 + Count64;

            private bool _anyProto3Optional;

            private void AccountFor(FieldDescriptorProto.Type type, string typeName, bool isProto3Optional)
            {
                if (isProto3Optional)
                    _anyProto3Optional = true;

                switch (type)
                {
                    case FieldDescriptorProto.Type.TypeBool:
                    case FieldDescriptorProto.Type.TypeEnum:
                    case FieldDescriptorProto.Type.TypeFixed32:
                    case FieldDescriptorProto.Type.TypeFloat:
                    case FieldDescriptorProto.Type.TypeInt32:
                    case FieldDescriptorProto.Type.TypeSfixed32:
                    case FieldDescriptorProto.Type.TypeSint32:
                    case FieldDescriptorProto.Type.TypeUint32:
                        Count32++;
                        break;
                    case FieldDescriptorProto.Type.TypeDouble:
                    case FieldDescriptorProto.Type.TypeFixed64:
                    case FieldDescriptorProto.Type.TypeInt64:
                    case FieldDescriptorProto.Type.TypeSfixed64:
                    case FieldDescriptorProto.Type.TypeSint64:
                    case FieldDescriptorProto.Type.TypeUint64:
                        Count32++;
                        Count64++;
                        break;
                    case FieldDescriptorProto.Type.TypeMessage:
                        switch(typeName)
                        {
                            case ".google.protobuf.Timestamp":
                            case ".google.protobuf.Duration":
                                Count64++;
                                break;
                            case ".bcl.Guid":
                                Count128++;
                                break;
                            default:
                                CountRef++;
                                break;
                        }
                        break;
                    default:
                        CountRef++;
                        break;
                }
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Readability")]
            internal string GetStorage(FieldDescriptorProto.Type type, string typeName)
            {
                switch (type)
                {
                    case FieldDescriptorProto.Type.TypeBool:
                        return "Boolean";
                    case FieldDescriptorProto.Type.TypeInt32:
                    case FieldDescriptorProto.Type.TypeSfixed32:
                    case FieldDescriptorProto.Type.TypeSint32:
                    case FieldDescriptorProto.Type.TypeEnum:
                        return "Int32";
                    case FieldDescriptorProto.Type.TypeFloat:
                        return "Single";
                    case FieldDescriptorProto.Type.TypeFixed32:
                    case FieldDescriptorProto.Type.TypeUint32:
                        return "UInt32";
                    case FieldDescriptorProto.Type.TypeDouble:
                        return "Double";
                    case FieldDescriptorProto.Type.TypeInt64:
                    case FieldDescriptorProto.Type.TypeSfixed64:
                    case FieldDescriptorProto.Type.TypeSint64:
                        return "Int64";
                    case FieldDescriptorProto.Type.TypeFixed64:
                    case FieldDescriptorProto.Type.TypeUint64:
                        return "UInt64";
                    case FieldDescriptorProto.Type.TypeMessage:
                        return typeName switch
                        {
                            ".google.protobuf.Timestamp" => "DateTime",
                            ".google.protobuf.Duration" => "TimeSpan",
                            ".bcl.Guid" => "Guid",
                            _ => "Object",
                        };
                    default:
                        return "Object";
                }
            }
            internal static OneOfStub[] Build(DescriptorProto message)
            {
                if (message.OneofDecls.Count == 0) return null;
                var stubs = new OneOfStub[message.OneofDecls.Count];
                int index = 0;
                foreach (var decl in message.OneofDecls)
                {
                    stubs[index] = new OneOfStub(decl, index);
                    index++;
                }
                foreach (var field in message.Fields)
                {
                    if (field.ShouldSerializeOneofIndex())
                    {
                        stubs[field.OneofIndex].AccountFor(field.type, field.TypeName, field.Proto3Optional);
                    }
                }
                return stubs;
            }
            private bool isFirst = true;
            internal bool IsFirst()
            {
                if (isFirst)
                {
                    isFirst = false;
                    return true;
                }
                return false;
            }

            internal string GetUnionType()
            {
                if (Count128 != 0)
                {
                    return CountRef == 0 ? "DiscriminatedUnion128" : "DiscriminatedUnion128Object";
                }
                if (Count64 != 0)
                {
                    return CountRef == 0 ? "DiscriminatedUnion64" : "DiscriminatedUnion64Object";
                }
                if (Count32 != 0)
                {
                    return CountRef == 0 ? "DiscriminatedUnion32" : "DiscriminatedUnion32Object";
                }
                return "DiscriminatedUnionObject";
            }

            internal bool IsProto3OptionalSyntheticOneOf()
                => CountTotal == 1 && _anyProto3Optional;
        }
    }
}
