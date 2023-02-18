using System;
using System.Diagnostics;
using Xunit;
using System.IO;

namespace Examples
{
    public static class PEVerify
    {
#if !COREFX
        static readonly string exePath;
        static readonly bool unavailable;
        static PEVerify()
        {
            exePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\PEVerify.exe");
            if (!File.Exists(exePath))
            {
                Console.Error.WriteLine("PEVerify not found at " + exePath);
                unavailable = true;
            }
        }
#endif
        public static void AssertValid(string path)
        {
#if COREFX
            return;
#else
            if (unavailable) return;
            if(!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            ProcessStartInfo psi = new ProcessStartInfo(exePath, path);
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process proc = Process.Start(psi))
            {
                if (proc.WaitForExit(20000))
                {
                    Assert.Equal(0, proc.ExitCode); //, path);
                    return; // proc.ExitCode == 0;
                }
                else
                {
                    try { proc.Kill(); } catch { }
                    throw new TimeoutException("PEVerify " + path);
                }
            }
#endif
        }
    }
}
