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
            WriteLine($"{reader.FieldNumber.ToString(FormatProvider)}: (unknown, {reader.WireType})");
            base.OnUnkownField(ref reader); // skip the value
        }

        protected override object OnBeginRepeated(FieldDescriptorProto field)
        {
            WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}=[ ({field.type})");
            return base.OnBeginRepeated(field);
        }
        protected override void OnEndRepeated(FieldDescriptorProto field)
        {
            base.OnEndRepeated(field);
            WriteLine($"] // {field.Name}, count: {Index.ToString(FormatProvider)}");
        }
        protected override void OnFieldFallback(FieldDescriptorProto field, string value)
        {
            if (Index < 0)
            {
                WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={value} ({field.type})");
            }
            else
            {
                WriteLine($"#{Index.ToString(FormatProvider)}={value}");
            }
        }

        protected override object OnBeginMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            if (field is not null)
            {
                if (Index < 0)
                {
                    WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={{");
                }
                else
                {
                    WriteLine($"#{Index.ToString(FormatProvider)}={{");
                }
            }
            return base.OnBeginMessage(field, message);
        }
        protected override void OnEndMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            base.OnEndMessage(field, message);
            if (field is not null)
            {
                WriteLine($"}} // {field.Name}");
            }
        }
    }
}
