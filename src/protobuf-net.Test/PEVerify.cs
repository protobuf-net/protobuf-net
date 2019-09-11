using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xunit;
using System.Diagnostics;

namespace ProtoBuf.unittest
{
    static class PEVerify
    {
#if COREFX
        public static ProtoBuf.Meta.TypeModel Compile(this ProtoBuf.Meta.RuntimeTypeModel model, string name, string path)
        {
            // dummy to avoid lots of test hackery for dll compilation tests
            model.CompileInPlace();
            return model;
        }
#else
        static readonly string exePath;
        static readonly bool unavailable;
        static PEVerify()
        {
            exePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\PEVerify.exe");
            if (!File.Exists(exePath))
            {
                Console.Error.WriteLine("PEVerify not found at " + exePath);
                unavailable = true;
            }
        }
#endif
        public static void Verify(string path)
        {
            Verify(path, 0, true);
        }
        public static void Verify(string path, int exitCode)
        {
            Verify(path, 0, true);
        }
        public static void Verify(string path, int exitCode, bool deleteOnSuccess)
        {
#if !COREFX
            if (unavailable) return;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            ProcessStartInfo psi = new ProcessStartInfo(exePath, path);
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process proc = Process.Start(psi))
            {
                if (proc.WaitForExit(10000))
                {
                    Assert.Equal(exitCode, proc.ExitCode); //, path);
                    if (deleteOnSuccess) try { File.Delete(path); } catch { }
                }
                else
                {
                    proc.Kill();
                    throw new TimeoutException("PEVerify " + path);
                }
            }
#endif
        }
    }
}
