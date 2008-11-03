using System;
using google.protobuf;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace ProtoBuf.CodeGenerator
{
    public static class InputFileLoader
    {
        public static void Merge(FileDescriptorSet files, string path)
        {
            if (files == null) throw new ArgumentNullException("files");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            bool deletePath = false;
            if(!IsValidBinary(path))
            { // try to use protoc
                path = CompileDescriptor(path);
                deletePath = true;
            }
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    Serializer.Merge(stream, files);
                }
            }
            finally
            {
                if(deletePath)
                {
                    try {File.Delete(path);}
                    catch {}
                }
            }
        }

        private static string CompileDescriptor(string path)
        {
            string tmp = Path.GetTempFileName();
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(
                    "protoc.exe", string.Format(
                                      @"""--descriptor_set_out={0}"" --include_imports ""{1}""",
                                      tmp, path));
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                string loaderPath = typeof (InputFileLoader).Assembly.CodeBase;
                
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = psi.RedirectStandardError = true;

                using (Process proc = Process.Start(psi))
                {
                    StringBuilder messages = new StringBuilder();
                    proc.ErrorDataReceived += (sender, args) => messages.Append(args.Data);
                    proc.OutputDataReceived += (sender, args) => messages.Append(args.Data);
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        Console.Error.Write(messages);
                        throw new ArgumentException("The input file could not be parsed.", "path");
                    }
                    return tmp;
                }
            }
            catch
            {
                try {File.Delete(tmp);} catch {}
                throw;
            }
        }

        static bool IsValidBinary(string path)
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    FileDescriptorSet file = Serializer.Deserialize<FileDescriptorSet>(stream);
                    return file != null;
                }
            }
            catch(ProtoException)
            {
                return false;
            }
        }
    }
}
