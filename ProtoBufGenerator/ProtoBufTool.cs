using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
//using ProtoBuf.CodeGenerator;

// namespace = <namespace>;propA,propB,propC,...

namespace ProtoBufGenerator
{
    [ComVisible(true)]
    [Guid("2095cd09-6c00-4ede-ad3f-adc5e971bb9b")]
    [CustomToolRegistration("ProtoBufTool", typeof(ProtoBufTool))]
    [ProvideObject(typeof(ProtoBufTool))]
    public class ProtoBufTool : CustomToolBase
    {
        protected string _sTmpPath;
        protected const string _sPGNamespace = "ProtoBufGenerator.";

        public ProtoBufTool()
        {
            // Build the tmp path...
            _sTmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // Extract the files we need to generate .cs
            ExtractProtoGen();

            // Listen for generate events
            this.OnGenerateCode += GenerationHandler;
        }

        ~ProtoBufTool()
        {
            // Cleanup
            CleaupProtoGen();
        }

        public void GenerationHandler(object s, GenerationEventArgs e)
        {
            // Fail on any error
            e.FailOnError = true;

            #region ensure protobuf-net.dll is in the project:

            try
            {
                VSProject project = (VSProject)e.ProjectItem.ContainingProject.Object;

                var pbNet = (from r in project.References.Cast<Reference>()
                             where r != null
                                && string.Compare(r.Name.ToLower(), "protobuf-net".ToLower(), true) == 0
                             select r).FirstOrDefault();

                if (pbNet == null)
                {
                    // Extract protobuf-net
                    string tempPath = Path.Combine(Path.GetDirectoryName(e.ProjectItem.ContainingProject.FullName), "protobuf-net.dll");
                    ExtractResource("ProtoBufGenerator.protobuf-net.dll", tempPath);
   
                    // add protobuf-net:
                    project.References.Add(tempPath);
                }
            }
            catch (Exception ex)
            {
                e.Errors.Add(
                    new GenerationError
                    {
                        Message = string.Format("The following exception occurred while trying to add protobuf-net:\r\n{0}", ex)
                    });
            }
            #endregion

            // Autogen Header
            OutputAutoGenHeader(e);

            // Extract the arguments from the namespace param
            // TODO: Is it possible to extend the "Properties" to have more fields
            //       so we don't need to abuse the namespace field?
            string sNamespace = e.Namespace;
            string sProps = string.Empty;
            List<string> xsltOptions = new List<string>();
            if(e.Namespace.Contains(';'))
            {
                sNamespace = e.Namespace.Split(';')[0];
                sProps = e.Namespace.Split(';')[1];
                string[] arrProps = sProps.Split(',');
                foreach(string sProp in arrProps)
                {
                    xsltOptions.Add(sProp.Trim());
                }
            }

            if (string.IsNullOrEmpty(sProps))
            {
                e.GenerateWarning("ProtoGen properties can be specified in the namespace property, example: \"MyNameSpace;xml,binary,detectMissing\" or \";xml,binary,detectMissing\" (must be after a semi-colon and comma seperated");
            }

            // Call ProtoGen to do the real work
            RunProtoGen(e, sProps, sNamespace, xsltOptions);
        }

        void RunProtoGen(GenerationEventArgs e, string sProps, string defaultNamespace, List<string> xsltOptions)
        {
            // Because protobuf freaks out if we pass a full path for the .proto
            // ("<fullpath.proto>: File does not reside within any path specified
            //  using --proto_path (or -I).  You must specify a --proto_path which
            //  encompasses this file.")
            // we instead pass only the .proto filename and set the working directory to match

            // Build the cmdline args
            string sArgs = String.Format("-i:{0} -t:csharp -q {1}", Path.GetFileName(e.InputFilePath), sProps);

            // Start the process
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(_sTmpPath, "ProtoGen.exe"), sArgs);
            Debug.WriteLine(psi.FileName, "ProtoBufGenerator");
            Debug.WriteLine(psi.Arguments, "ProtoBufGenerator");

            using (StringWriter messages = new StringWriter())
            {
                try {
                    /*
                    CommandLineOptions opt = new CommandLineOptions(messages);
                    opt.InPaths.Add(_sTmpPath);
                    opt.DefaultNamespace = defaultNamespace;
                    opt.OutPath = "-";
                    opt.ShowHelp = false;
                    opt.ShowLogo = false;
                    opt.Template = "csharp";
                    opt.DefaultNamespace = "foo";
                    foreach (string s in xsltOptions)
                    {
                        opt.XsltOptions.AddParam(s,"","true");
                    }
                    opt.Execute();
                    // We succeeded, add the code
                    e.OutputCode.Append(opt.Code);
                    string warn = messages.ToString();
                    if (!string.IsNullOrEmpty(warn))
                    {
                        e.GenerateWarning(warn);
                    }*/
                } catch {
                    e.GenerateError(messages.ToString());
                }
            }
        }

        void OutputAutoGenHeader(GenerationEventArgs e)
        {
            e.OutputCode.AppendLine("//------------------------------------------------------------------------------");
            e.OutputCode.AppendLine("// <auto-generated>");
            e.OutputCode.AppendLine("//     This code was generated by a tool.");
            e.OutputCode.AppendLine(String.Format("//     Runtime Version:{0}", RuntimeEnvironment.GetSystemVersion()));
            e.OutputCode.AppendLine("//");
            e.OutputCode.AppendLine("//     Changes to this file may cause incorrect behavior and will be lost if");
            e.OutputCode.AppendLine("//     the code is regenerated.");
            e.OutputCode.AppendLine("// </auto-generated>");
            e.OutputCode.AppendLine("//------------------------------------------------------------------------------");
            e.OutputCode.AppendLine();
        }

        void ExtractResource(string sResName, string sFileName)
        {
            byte[] buffer = typeof(ProtoBufTool).Assembly.GetManifestResourceStream(sResName).ReadToEnd();
            File.WriteAllBytes(sFileName, buffer);
        }

        void ExtractProtoGen()
        {
            if(!Directory.Exists(_sTmpPath))
                Directory.CreateDirectory(_sTmpPath);

            // Find ProtoGen related resources
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in executingAssembly.GetManifestResourceNames())
            {
                if (resourceName.StartsWith(_sPGNamespace))
                {
                    string sFileName = Path.Combine(_sTmpPath, resourceName.Remove(0, _sPGNamespace.Length));
                    if(!File.Exists(sFileName))
                        ExtractResource(resourceName, sFileName);
                }
            }
        }

        void CleaupProtoGen()
        {
            if (Directory.Exists(_sTmpPath))
                Directory.Delete(_sTmpPath, true);
        }

        static ThreadStart DumpStream(TextReader reader, TextWriter writer)
        {
            return (ThreadStart)delegate
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    //Debug.WriteLine(line);
                    writer.WriteLine(line);
                }
            };
        }
    }
}
