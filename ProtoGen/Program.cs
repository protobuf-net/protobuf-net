using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using google.protobuf;

namespace ProtoBuf.CodeGenerator
{
    static class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Main(string[] args)
        {
            try
            {
                Generate(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                return 1;
            }
        }


        static void Generate(string[] args)
        {
            CommandLineOptions options = CommandLineOptions.Parse(args);

            string xml = LoadFilesAsXml(options);
            string code = ApplyTransform(options, xml);
            if(!string.IsNullOrEmpty(options.OutPath))
            {
                File.WriteAllText(options.OutPath, code);
            }

            //if (options.TestCompile)
            //{
            //    TestCompile(options, code);
            //}
            
            if (string.IsNullOrEmpty(options.OutPath))
            {
                Console.Out.Write(code);
            }

    }

        //private static void TestCompile(GenerationOptions options, string code) {
        //    CompilerResults results;
        //    switch(options.Template) {
        //        case GenerationOptions.TEMPLATE_CSHARP:
        //            {
        //                CSharpCodeProvider csc = new CSharpCodeProvider();
        //                string[] refs = new string[] { "System.dll", "System.Xml.dll", "protobuf-net.dll" };
        //                CompilerParameters cscArgs = new CompilerParameters(refs, "descriptor.dll", false);
        //                results = csc.CompileAssemblyFromSource(cscArgs, code);
        //                break;
        //            }
        //        default:
        //            Console.Error.WriteLine("No compiler available to test code with template " + options.Template);
        //            return;
        //    }
        //    ShowErrors(results.Errors);
        //}

        private static string ApplyTransform(CommandLineOptions options, string xml) {
            XmlWriterSettings settings = new XmlWriterSettings
                                         {
                                             ConformanceLevel = ConformanceLevel.Auto, CheckCharacters = false
                                         };
            StringBuilder sb = new StringBuilder();
            using(XmlReader reader = XmlReader.Create(new StringReader(xml)))
            using(TextWriter writer = new StringWriter(sb))
            {

                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(Path.ChangeExtension(options.Template, "xslt"));
                xslt.Transform(reader, options.XsltOptions, writer);
            }
            return sb.ToString();
        }

        private static string LoadFilesAsXml(CommandLineOptions options)
        {
            FileDescriptorSet set = new FileDescriptorSet();

            foreach (string inPath in options.InPaths) {
                InputFileLoader.Merge(set, inPath);
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
            return sb.ToString();
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
