using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using google.protobuf;
using Microsoft.CSharp;
using ProtoBuf;
namespace ProtoGen
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

        class GenerationOptions
        {
            public bool TestCompile { get; set; }
            public string Template { get; set; }
            public string OutPath { get; set; }
            public XsltArgumentList XsltOptions { get; private set; }
            public List<string> InPaths { get; private set; }

            public GenerationOptions()
            {
                Template = TEMPLATE_CSHARP;
                XsltOptions = new XsltArgumentList();
                InPaths = new List<string>();
            }

            public const string TEMPLATE_CSHARP = "csharp";

        }
        static void Generate(string[] args)
        {
            GenerationOptions options = ParseCommandOptions(args);

            string xml = LoadFilesAsXml(options);
            string code = ApplyTransform(options, xml);
            if(!string.IsNullOrEmpty(options.OutPath))
            {
                File.WriteAllText(options.OutPath, code);
            }

            if (options.TestCompile)
            {
                TestCompile(options, code);
            }
            
            if (string.IsNullOrEmpty(options.OutPath))
            {
                Console.Out.Write(code);
            }

    }

        private static void TestCompile(GenerationOptions options, string code) {
            CompilerResults results;
            switch(options.Template) {
                case GenerationOptions.TEMPLATE_CSHARP:
                    {
                        CSharpCodeProvider csc = new CSharpCodeProvider();
                        string[] refs = new string[] { "System.dll", "System.Xml.dll", "protobuf-net.dll" };
                        CompilerParameters cscArgs = new CompilerParameters(refs, "descriptor.dll", false);
                        results = csc.CompileAssemblyFromSource(cscArgs, code);
                        break;
                    }
                default:
                    Console.Error.WriteLine("No compiler available to test code with template " + options.Template);
                    return;
            }
            ShowErrors(results.Errors);
        }

        private static string ApplyTransform(GenerationOptions options, string xml) {
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

        private static string LoadFilesAsXml(GenerationOptions options) {
            FileDescriptorSet set = new FileDescriptorSet();

            foreach (string inPath in options.InPaths) {
                using (Stream file = File.OpenRead(inPath))
                {
                    set = Serializer.Merge(file, set);
                }
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

        static readonly char[] SplitTokens = { '=' };
        private static void Split(string arg, out string key, out string value)
        {
            string[] parts = arg.Trim().Split(SplitTokens, 2);
            key = parts[0].Trim();
            value = parts.Length > 1 ? parts[1].Trim() : null;
                    
        }
        private static GenerationOptions ParseCommandOptions(string[] args) {
            GenerationOptions options = new GenerationOptions();

            string key, value;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].Trim();
                if (arg.StartsWith("-o:"))
                {
                    if (options.OutPath != null) throw new InvalidOperationException("Only one output path can be specified.");
                    options.OutPath = arg.Substring(3).Trim();
                }
                else if (arg.StartsWith("-p:"))
                {
                    Split(arg.Substring(3),out key, out value);
                    options.XsltOptions.AddParam(key, "", value ?? "true");
                }
                else if (arg.StartsWith("-t:"))
                {
                    options.Template = arg.Substring(3).Trim();
                }
                else if (arg=="-c")
                {
                    options.TestCompile = true;
                }
                else if(arg.StartsWith("-i:"))
                {
                    options.InPaths.Add(arg.Substring(3).Trim());
                }
            }
            if (options.InPaths.Count == 0) throw new InvalidOperationException("No input files specified.");
            return options;
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
