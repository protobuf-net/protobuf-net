using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Xsl;
using google.protobuf;
using System.Xml;
using System.Text;
using System.Xml.Serialization;

namespace ProtoBuf.CodeGenerator
{
    public sealed class CommandLineOptions
    {
        public string Template { get; set; }
        public bool NoLogo { get; set; }
        public string OutPath { get; set; }
        public bool ShowHelp { get; set; }
        public XsltArgumentList XsltOptions { get; private set; }
        public List<string> InPaths { get; private set; }

        private readonly TextWriter messageOutput;

        public static CommandLineOptions Parse(TextWriter messageOutput, string[] args)
        {
            CommandLineOptions options = new CommandLineOptions(messageOutput);

            string key, value;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].Trim();
                if (arg.StartsWith("-o:"))
                {
                    if (!string.IsNullOrEmpty(options.OutPath)) options.ShowHelp = true;
                    options.OutPath = arg.Substring(3).Trim();
                }
                else if (arg.StartsWith("-p:"))
                {
                    Split(arg.Substring(3), out key, out value);
                    options.XsltOptions.AddParam(key, "", value ?? "true");
                }
                else if (arg.StartsWith("-t:"))
                {
                    options.Template = arg.Substring(3).Trim();
                }
                else if (arg == "/?" || arg == "-h")
                {
                    options.ShowHelp = true;
                }
                else if (arg == "-q") // quiet
                {
                    options.NoLogo = true;
                }
                else if (arg.StartsWith("-i:"))
                {
                    options.InPaths.Add(arg.Substring(3).Trim());
                }
                else
                {
                    options.ShowHelp = true;
                }
            }
            if (options.InPaths.Count == 0) options.ShowHelp = true;
            return options;

        }

        static readonly char[] SplitTokens = { '=' };
        private static void Split(string arg, out string key, out string value)
        {
            string[] parts = arg.Trim().Split(SplitTokens, 2);
            key = parts[0].Trim();
            value = parts.Length > 1 ? parts[1].Trim() : null;
        }

        private CommandLineOptions(TextWriter messageOutput)
        {
            if(messageOutput == null) throw new ArgumentNullException("messageOutput");
            this.messageOutput = messageOutput;
            Template = TemplateCSharp;
            OutPath = "";
            XsltOptions = new XsltArgumentList();
            XsltOptions.XsltMessageEncountered += XsltOptions_XsltMessageEncountered;
            InPaths = new List<string>();
        }

        void XsltOptions_XsltMessageEncountered(object sender, XsltMessageEncounteredEventArgs e)
        {
            messageOutput.WriteLine(e.Message);
        }

        public const string TemplateCSharp = "csharp";

        public void Execute()
        {
            if(!NoLogo)
            {
                messageOutput.WriteLine("protobuf-net:protogen - code generator for .proto");
            }
            if(ShowHelp)
            {
                messageOutput.WriteLine("usage: protogen -i:{infile2} [-i:{infile2}] [-o:{outfile}] [-t:{template}] [-p:{prop}] [-p:{prop}={value}]");
                return;
            }
            string xml = LoadFilesAsXml(this);
            string code = ApplyTransform(this, xml);
            if (!string.IsNullOrEmpty(this.OutPath))
            {
                File.WriteAllText(this.OutPath, code);
            }
            if (string.IsNullOrEmpty(this.OutPath))
            {
                messageOutput.Write(code);
            }

        }


        private static string LoadFilesAsXml(CommandLineOptions options)
        {
            FileDescriptorSet set = new FileDescriptorSet();

            foreach (string inPath in options.InPaths)
            {
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

        private static string ApplyTransform(CommandLineOptions options, string xml)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Auto,
                CheckCharacters = false
            };
            
            StringBuilder sb = new StringBuilder();
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            using (TextWriter writer = new StringWriter(sb))
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(Path.ChangeExtension(options.Template, "xslt"));
                xslt.Transform(reader, options.XsltOptions, writer);
            }
            return sb.ToString();
        }
    }
}
