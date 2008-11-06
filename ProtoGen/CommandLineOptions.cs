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
        private string template = TemplateCSharp, outPath = "";
        private bool noLogo, showHelp;
        private readonly List<string> inPaths = new List<string>();
        public string Template { get { return template;} set { template = value;} }
        public bool NoLogo { get { return noLogo; } set { noLogo = value; } }
        public string OutPath { get { return outPath; } set { outPath = value; } }
        public bool ShowHelp { get { return showHelp; } set { showHelp = value; } }
        private readonly XsltArgumentList xsltOptions = new XsltArgumentList();
        public XsltArgumentList XsltOptions { get { return xsltOptions; } }
        public List<string> InPaths { get { return inPaths; } }

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
            if (options.InPaths.Count == 0)
            {
                options.ShowHelp = (string) options.XsltOptions.GetParam("help", "") != "true";
            }
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

            // handling this (even trivially) suppresses the default write
            XsltOptions.XsltMessageEncountered += delegate { };
        }

        public const string TemplateCSharp = "csharp";

        public void Execute()
        {
            if(!NoLogo)
            {
                messageOutput.WriteLine(Properties.Resources.LogoText);
            }
            if(ShowHelp)
            {
                messageOutput.WriteLine(Properties.Resources.Usage);
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
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineHandling = NewLineHandling.Entitize;
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                xser.Serialize(writer, set);
            }
            return sb.ToString();
        }

        private static string ApplyTransform(CommandLineOptions options, string xml)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Auto;
            settings.CheckCharacters = false;
            
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
