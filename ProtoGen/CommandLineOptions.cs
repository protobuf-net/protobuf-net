using System.Collections.Generic;
using System.Xml.Xsl;

namespace ProtoBuf.CodeGenerator
{
    public sealed class CommandLineOptions
    {
        public string Template { get; set; }
        public string OutPath { get; set; }
        public bool ShowHelp { get; set; }
        public XsltArgumentList XsltOptions { get; private set; }
        public List<string> InPaths { get; private set; }

        public static CommandLineOptions Parse(string[] args)
        {
            CommandLineOptions options = new CommandLineOptions();
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
                //else if (arg == "-c")
                //{
                //    options.TestCompile = true;
                //}
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

        private CommandLineOptions()
        {
            Template = TemplateCSharp;
            OutPath = "";
            XsltOptions = new XsltArgumentList();
            InPaths = new List<string>();
        }

        public const string TemplateCSharp = "csharp";
    }
}
