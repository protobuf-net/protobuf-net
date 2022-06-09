using Google.Protobuf.Reflection;
using System;
using System.IO;

namespace ProtoBuf.Internal
{
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
