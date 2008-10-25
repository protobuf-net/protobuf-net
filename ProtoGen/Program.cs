using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using google.protobuf;
using ProtoBuf;
using System.Xml.Xsl;
using System;
using System.Runtime.CompilerServices;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
namespace ProtoGen
{
    static class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Main()
        {
            try
            {
                Generate();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                return 1;
            }
        }
        static void Generate() {
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

            sb = new StringBuilder();
            settings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Auto, CheckCharacters = false
            };
            using(XmlReader reader = XmlReader.Create(new StringReader(xml)))

            using(TextWriter writer = new StringWriter(sb))
            {

                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load("csharp.xslt");
                xslt.Transform(reader,null,writer);
            }

            string code = sb.ToString();
            File.WriteAllText("descriptor.cs", code);

            CSharpCodeProvider csc = new CSharpCodeProvider();
            string[] refs = new string[] {"System.dll", "System.Xml.dll", "protobuf-net.dll"};
            CompilerParameters args = new CompilerParameters(refs, "descriptor.dll", false);
            CompilerResults results = csc.CompileAssemblyFromSource(args, code);
            ShowErrors(results.Errors);
            
            Console.Out.Write(code);
            
        }
        static void ShowErrors(CompilerErrorCollection errors)
        {
            if(errors.Count > 0)
            {
                Console.Error.Write(errors.Count + " errors");
                foreach(CompilerError err in errors)
                {
                    Console.Error.Write(err.IsWarning ? "Warning: " : "Error: ");
                    Console.Error.WriteLine(err.ErrorText);
                }
            }
        }
    }
}
