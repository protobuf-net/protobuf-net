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

        public static void Merge(FileDescriptorSet files, string path, TextWriter stderr, params string[] args)
        {
            if (stderr == null) throw new ArgumentNullException("stderr");
            if (files == null) throw new ArgumentNullException("files");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            
            bool deletePath = false;
            if(!IsValidBinary(path))
            { // try to use protoc
                path = CompileDescriptor(path, stderr, args);
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
        public static string GetProtocPath(out string folder)
        {
            const string Name = "protoc.exe";
            string lazyPath = InputFileLoader.CombinePathFromAppRoot(Name);
            if (File.Exists(lazyPath))
            {   // use protoc.exe from the existing location (faster)
                folder = null;
                return lazyPath;
            }
            folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, Name);
            
            // look inside ourselves...
            using(Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                typeof(InputFileLoader).Namespace + "." + Name))
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
        
        private static string CompileDescriptor(string path, TextWriter stderr, params string[] args)
        {
            
            string tmp = Path.GetTempFileName();
            string tmpFolder = null, protocPath = null;
            try
            {
                protocPath = GetProtocPath(out tmpFolder);
                ProcessStartInfo psi = new ProcessStartInfo(
                    protocPath,
                    string.Format(@"""--descriptor_set_out={0}"" ""--proto_path={1}"" ""--proto_path={2}"" --error_format=gcc ""{3}"" {4}",
                             tmp, // output file
                             Environment.CurrentDirectory, // primary search path
                             Path.GetDirectoryName(protocPath), // secondary search path
                             Path.Combine(Environment.CurrentDirectory, path), // input file
                             string.Join(" ", args) // extra args
                    )
                );
                Debug.WriteLine(psi.FileName + " " + psi.Arguments, "protoc");

                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.WorkingDirectory = Environment.CurrentDirectory;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = psi.RedirectStandardError = true;

                using (Process proc = Process.Start(psi))
                {
                    Thread errThread = new Thread(DumpStream(proc.StandardError, stderr));
                    Thread outThread = new Thread(DumpStream(proc.StandardOutput, stderr));
                    errThread.Name = "stderr reader";
                    outThread.Name = "stdout reader";
                    errThread.Start();
                    outThread.Start();
                    proc.WaitForExit();
                    outThread.Join();
                    errThread.Join();
                    if (proc.ExitCode != 0)
                    {
                        if (HasByteOrderMark(path))
                        {
                            stderr.WriteLine("The input file should be UTF8 without a byte-order-mark (in Visual Studio use \"File\" -> \"Advanced Save Options...\" to rectify)");
                        }
                        throw new ProtoParseException(Path.GetFileName(path));
                    }
                    return tmp;
                }
            }
            catch
            {
                try { if(File.Exists(tmp)) File.Delete(tmp); }
                catch { } // swallow
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tmpFolder))
                {
                    try { Directory.Delete(tmpFolder, true); }
                    catch { } // swallow
                }
                
            }
        }

        private static bool HasByteOrderMark(string path)
        {
            try
            {
                using (Stream s = File.OpenRead(path))
                {
                    return s.ReadByte() > 127;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex); // log only
                return false;
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
            catch
            {
                return false;
            }
        }
    }
    public sealed class ProtoParseException : Exception
    {
        public ProtoParseException(string file) : base("An error occurred parsing " + file) { }
    }
}
