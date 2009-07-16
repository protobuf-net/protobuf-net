using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace ProtoBufGenerator
{
    [ComVisible(true)]
    [DefaultRegistryRoot(@"Software\Microsoft\VisualStudio\9.0")]
    [PackageRegistration(UseManagedResourcesOnly=true, RegisterUsing=RegistrationMethod.CodeBase)]
    [InstalledProductRegistration(false,"#102","#103","1.0", IconResourceID=100)]
    [ProvideLoadKey("Standard", "1.0", "protobuf-net", "Marc Gravell",101)]
    [Guid("3128D6BC-FF6D-4b39-98F4-681146FD7623")]
    [CustomToolRegistration("ProtoBufTool", typeof(ProtoBufTool), FileExtension=".proto")]
    [ProvideObject(typeof(ProtoBufTool))]
    public class ProtoBufTool : CustomToolBase
    {
        public ProtoBufTool()
        {
            // Listen for generate events
            this.OnGenerateCode += GenerationHandler; 
        }
        static string installPath;
        public static string GetInstallPath(string filename)
        {            
            if (installPath == null)
            {
                installPath = Path.GetDirectoryName(typeof(ProtoBufTool).Assembly.Location);
            }
            return string.IsNullOrEmpty(filename) ? installPath : Path.Combine(installPath, filename);
        }
        const string PBNETDLL = "protobuf-net.dll";

        static string[] GetProtoGenArgs(string inputPath, string outputPath, string language, string namespaceOptions)
        {
            List<string> args = new List<string>();
            switch(language) {
                case ".cs":
                    language = "csharp"; 
                    break;
                default:
                    language = language.TrimStart('.');
                    break;
            }
            args.Add("-writeErrors"); // include error output in outputPath
            args.Add("-i:" + inputPath); // input
            args.Add("-w:" + Path.GetDirectoryName(inputPath)); // working path
            args.Add("-t:" + language); // template
            args.Add("-o:" + outputPath); // output
            args.Add("-q"); // quiet
            string[] parts = namespaceOptions.Split(';');
            if(parts.Length > 0) args.Add("-ns:" + parts[0]); // default namespace
            for(int i = 1 ; i < parts.Length ; i++)
            {
                args.Add("-p:" + parts[i]); // parameter
            }
            return args.ToArray();
        }
        public void GenerationHandler(object s, GenerationEventArgs e)
        {
            // Fail on any error
            e.FailOnError = true;
            
            string tmp = Path.GetTempFileName();
            try
            {
                string[] args = GetProtoGenArgs(e.InputFilePath, tmp, e.OutputFileExtension, e.Namespace);
                bool writeWhatWeDid = true;
                string root = GetInstallPath(null), app = GetInstallPath("protogen.exe");
                if (!File.Exists(app))
                {
                    e.GenerateError("Missing: " + app);
                    return;
                }

                int result;

                ProcessStartInfo psi = new ProcessStartInfo(app);
                psi.CreateNoWindow = true;
                psi.WorkingDirectory = root;
                StringBuilder sb = new StringBuilder();
                foreach (string arg in args)
                {
                    sb.Append("\"" + arg + "\" ");
                }
                psi.Arguments = sb.ToString();
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process proc = Process.Start(psi))
                {
                    proc.WaitForExit();
                    result = proc.ExitCode;
                }

                /*AppDomain ad = AppDomain.CreateDomain("protogen", null, root, "", false);
                
                try
                {
                    result = ad.ExecuteAssembly(app, null, args);
                    writeWhatWeDid = result != 0;
                }
                finally
                {
                    AppDomain.Unload(ad);
                }*/

                if (writeWhatWeDid)
                {
#if DEBUG2
                    e.GenerateWarning(app);
                    foreach (string arg in args) e.GenerateWarning(arg);
#endif
                }

                using(var lineReader = File.OpenText(tmp))
                {
                    string line;
                    switch(result)
                    {
                        case 0:
                            while((line = lineReader.ReadLine()) != null) {
                                e.OutputCode.AppendLine(line);
                            }
                            break;
                        default:
                            bool hasErrorText = false;
                            while((line = lineReader.ReadLine()) != null) {
                                GenerateErrorWithPosition(line, e);
                                hasErrorText = true;
                            }
                            if(!hasErrorText) {
                                e.GenerateError("Code generation failed with exit-code " + result);
                            }
                            break;
                    }
                }
                #region ensure protobuf-net.dll is in the project:

                try
                {
                    VSProject project = (VSProject)e.ProjectItem.ContainingProject.Object;
                    bool hasRef = project.References.Cast<Reference>()
                        .Any(r => r != null && string.Equals(r.Name, "protobuf-net", StringComparison.InvariantCultureIgnoreCase));

                    if (!hasRef)
                    {
                        string toolPath = GetInstallPath(PBNETDLL);

                        /* REMOVED: copy dll into local project
                        string projectPath = Path.Combine(
                                Path.GetDirectoryName(e.ProjectItem.ContainingProject.FullName),
                                PBNETDLL);
                        // copy it into the project if needed
                        if(!File.Exists(projectPath))
                        {
                            File.Copy(toolPath, projectPath);
                        }
                         */
                        // add the reference (from the install location)
                        Reference dllRef = project.References.Add(toolPath);
                        dllRef.CopyLocal = true;
                    }
                }
                catch (Exception ex)
                {
                    e.GenerateWarning("Failed to add reference to protobuf-net:" + ex.Message);
                }
                #endregion
            }
            finally
            {
                try { File.Delete(tmp); }
                catch (Exception ex) { e.GenerateWarning(ex.Message); }
            }
        }

        static readonly Regex gccErrorFormat = new Regex(@"\:([0-9]+)\:([0-9]+)\:", RegexOptions.Compiled);
        static void GenerateErrorWithPosition(string line, GenerationEventArgs e)
        {
            Match match = gccErrorFormat.Match(line);
            int row, col;
            if(match.Success && int.TryParse(match.Groups[1].Value, out row)
                && int.TryParse(match.Groups[2].Value, out col)) {
                e.GenerateError(line, row-1, col-1); // note offsets!
            } else {
                e.GenerateError(line);
            }
        }
    }
}
