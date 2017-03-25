using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using System.Diagnostics;

namespace ProtoBuf.unittest
{
    static class PEVerify
    {
#if !COREFX
        static readonly string exePath;
        static readonly bool unavailable;
        static PEVerify()
        {
            exePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\PEVerify.exe");
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
                    Assert.AreEqual(exitCode, proc.ExitCode, path);
                    if(deleteOnSuccess) File.Delete(path);
                }
                else
                {
                    proc.Kill();
                    throw new TimeoutException();
                }
            }
#endif
        }
    }
}
