using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Schemas
{
    public class SchemaTests
    {
        private ITestOutputHelper _output;
        public SchemaTests(ITestOutputHelper output) => _output = output;
        [Theory]
        [InlineData(@"Schemas\descriptor.proto")]        
        public void CanParse(string path)
        {
            Schema schema;
            using (var proto = File.OpenText(path))
            {
                schema = Schema.Parse(proto);
            }
            foreach (var msg in schema.Messages)
            {
                WriteMessage(msg, 0);
            }
        }
        private string Indent(int count) => new string(' ', count);
        private void WriteMessage(Message msg, int indent)
        {
            _output.WriteLine($"{Indent(indent++)}{msg}");
            foreach (var field in msg.Fields)
            {
                _output.WriteLine($"{Indent(indent)}{field}");
            }
            foreach (var res in msg.Reservations)
            {               
                _output.WriteLine($"{Indent(indent)}-{res}");
            }
            foreach (var subMsg in msg.Messages)
            {
                WriteMessage(subMsg, indent);
            }
        }
    }
}
