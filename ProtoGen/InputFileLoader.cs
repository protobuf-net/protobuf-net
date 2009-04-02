using System;
using google.protobuf;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ProtoBuf.CodeGenerator
{
    public static class InputFileLoader
    {
        public static void Merge(FileDescriptorSet files, string path, params string[] args)
        {
            if (files == null) throw new ArgumentNullException("files");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            bool deletePath = false;
            if(!IsValidBinary(path))
            { // try to use protoc
                path = CompileDescriptor(path, args);
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
                    File.Delete(path);
                }
            }
        }

        private static string CompileDescriptor(string path, params string[] args)
        {
            string tmp = Path.GetTempFileName();
            
            try
            {
                string loaderPath = typeof(InputFileLoader).Assembly.CodeBase;
                
                loaderPath = Path.Combine(Path.GetDirectoryName(loaderPath), "protoc.exe");
                if(loaderPath.StartsWith(@"file:\")) {
                    loaderPath = loaderPath.Substring(6);
                }
                ProcessStartInfo psi = new ProcessStartInfo(
                    loaderPath, string.Format(
                                      @"""--descriptor_set_out={0}"" ""{1}"" {2}",
                                      tmp, path, string.Join(" ", args)));
                Debug.WriteLine(psi.FileName, "protoc");
                Debug.WriteLine(psi.Arguments, "protoc");
                
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.WorkingDirectory = Environment.CurrentDirectory;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = psi.RedirectStandardError = true;

                using (Process proc = Process.Start(psi))
                {
                    Thread stdErr = new Thread(DumpStream(proc.StandardError, Console.Error));
                    Thread stdOut = new Thread(DumpStream(proc.StandardOutput, Console.Out));
                    stdErr.Name = "stderr reader";
                    stdOut.Name = "stdout reader";
                    stdErr.Start();
                    stdOut.Start();
                    proc.WaitForExit();
                    stdOut.Join();
                    stdErr.Join();
                    if (proc.ExitCode != 0)
                    {
                        throw new ArgumentException("The input file could not be parsed.", "path");
                    }
                    return tmp;
                }
            }
            catch
            {
                File.Delete(tmp);
                throw;
            }
        }

        static ThreadStart DumpStream(TextReader reader, TextWriter writer)
        {
            return (ThreadStart) delegate
             {
                 string line;
                 while ((line = reader.ReadLine()) != null) writer.WriteLine(line);
             };
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
