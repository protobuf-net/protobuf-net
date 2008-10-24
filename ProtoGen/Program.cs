using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using google.protobuf;
using ProtoBuf;
namespace ProtoGen
{
    static class Program
    {
        static void Main()
        {
            FileDescriptorSet set;

            using (Stream file = File.OpenRead("descriptor.bin"))
            {
                set = Serializer.Deserialize<FileDescriptorSet>(file);
            }
            XmlSerializer xser = new XmlSerializer(typeof(FileDescriptorSet));
            XmlWriterSettings settings = new XmlWriterSettings
                                         {
                                             Indent = true,
                                             IndentChars = "  ",
                                             NewLineHandling = NewLineHandling.Entitize
                                         };
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                xser.Serialize(writer, set);
            }
            string xml = sb.ToString();
            File.WriteAllText("descriptor.xml", xml);
        }
    }
}
