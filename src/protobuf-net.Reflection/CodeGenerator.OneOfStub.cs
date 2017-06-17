using Google.Protobuf.Reflection;

namespace ProtoBuf.Reflection
{
    partial class CommonCodeGenerator
    {
        protected class OneOfStub
        {
            public OneofDescriptorProto OneOf { get; }

            internal OneOfStub(GeneratorContext context, OneofDescriptorProto decl)
            {
                OneOf = decl;
                //context.
            }
            public int Count32 { get; private set; }
            public int Count64 { get; private set; }
            public int CountRef { get; private set; }
            public int CountTotal => CountRef + Count32 + Count64;

            private void AccountFor(FieldDescriptorProto.Type type)
            {
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
                    default:
                        CountRef++;
                        break;
                }
            }
            internal string GetStorage(FieldDescriptorProto.Type type)
            {
                switch (type)
                {
                    case FieldDescriptorProto.Type.TypeBool:
                        return nameof(DiscriminatedUnion64Object.Boolean);
                    case FieldDescriptorProto.Type.TypeInt32:
                    case FieldDescriptorProto.Type.TypeSfixed32:
                    case FieldDescriptorProto.Type.TypeSint32:
                    case FieldDescriptorProto.Type.TypeFixed32:
                    case FieldDescriptorProto.Type.TypeEnum:
                        return nameof(DiscriminatedUnion64Object.Int32);
                    case FieldDescriptorProto.Type.TypeFloat:
                        return nameof(DiscriminatedUnion64Object.Single);
                    case FieldDescriptorProto.Type.TypeUint32:
                        return nameof(DiscriminatedUnion64Object.UInt32);
                    case FieldDescriptorProto.Type.TypeDouble:
                        return nameof(DiscriminatedUnion64Object.Double);
                    case FieldDescriptorProto.Type.TypeFixed64:
                    case FieldDescriptorProto.Type.TypeInt64:
                    case FieldDescriptorProto.Type.TypeSfixed64:
                    case FieldDescriptorProto.Type.TypeSint64:
                        return nameof(DiscriminatedUnion64Object.Int64);
                    case FieldDescriptorProto.Type.TypeUint64:
                        return nameof(DiscriminatedUnion64Object.UInt64);
                    default:
                        return nameof(DiscriminatedUnion64Object.Object);
                }
            }
            internal static OneOfStub[] Build(GeneratorContext context, DescriptorProto message)
            {
                if (message.OneofDecls.Count == 0) return null;
                var stubs = new OneOfStub[message.OneofDecls.Count];
                int index = 0;
                foreach (var decl in message.OneofDecls)
                {
                    stubs[index++] = new OneOfStub(context, decl);
                }
                foreach (var field in message.Fields)
                {
                    if (field.ShouldSerializeOneofIndex())
                    {
                        stubs[field.OneofIndex].AccountFor(field.type);
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
                if (Count64 != 0)
                {
                    return CountRef == 0 ? nameof(DiscriminatedUnion64) : nameof(DiscriminatedUnion64Object);
                }
                if (Count32 != 0)
                {
                    return CountRef == 0 ? nameof(DiscriminatedUnion32) : nameof(DiscriminatedUnion32Object);
                }
                return nameof(DiscriminatedUnionObject);
            }
        }
    }
}
