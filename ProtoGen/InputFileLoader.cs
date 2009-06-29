using System;
using google.protobuf;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Reflection;

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

        public static string CombinePathFromAppRoot(string path)
        {
            string loaderPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            if (!string.IsNullOrEmpty(loaderPath)
                && loaderPath[loaderPath.Length - 1] != Path.DirectorySeparatorChar
                && loaderPath[loaderPath.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                loaderPath += Path.DirectorySeparatorChar;
            }
            if (loaderPath.StartsWith(@"file:\"))
            {
                loaderPath = loaderPath.Substring(6);
            }
            return Path.Combine(Path.GetDirectoryName(loaderPath), path);   
        }
        public static string ExtractResourceToTempFolder(string name, out string folder)
        {
            folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, name);
            using(Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                typeof(InputFileLoader).Namespace + "." + name))
            using(Stream outFile = File.OpenWrite(path))
            {
                long len = 0;
                int bytesRead;
                byte[] buffer = new byte[4096];
                while((bytesRead = resStream.Read(buffer, 0, buffer.Length)) > 0) {
                    outFile.Write(buffer, 0, bytesRead);
                    len += bytesRead;
                }
                outFile.SetLength(len);
            }
            return path;
        }
        private static string CompileDescriptor(string path, params string[] args)
        {
            string tmp = Path.GetTempFileName();
            string tmpFolder = null, protocPath = null;
            try
            {
                protocPath = ExtractResourceToTempFolder("protoc.exe", out tmpFolder);
                ProcessStartInfo psi = new ProcessStartInfo(
                    protocPath,
                    string.Format(
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
                try { File.Delete(tmp); }
                catch { } // swallow
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(protocPath))
                {
                    try { File.Delete(protocPath); }
                    catch { } // swallow
                }
                if (!string.IsNullOrEmpty(tmpFolder))
                {
                    try { Directory.Delete(tmpFolder); }
                    catch { } // swallow
                }
                
            }
        }

        static ThreadStart DumpStream(TextReader reader, TextWriter writer)
        {
            return (ThreadStart) delegate
             {
                 string line;
                 while ((line = reader.ReadLine()) != null)
                 {
                     Debug.WriteLine(line);
                     writer.WriteLine(line);
                 }
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
