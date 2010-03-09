using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;

namespace Examples
{
    public static class PEVerify
    {
        public static void AssertValid(string path)
        {
            // note; PEVerify can be found %ProgramFiles%\Microsoft SDKs\Windows\v6.0A\bin
            const string exePath = "PEVerify.exe";
            using (Process proc = Process.Start(exePath, path))
            {
                if (proc.WaitForExit(10000))
                {
                    Assert.AreEqual(0, proc.ExitCode, path);
                }
                else
                {
                    proc.Kill();
                    throw new TimeoutException();
                }
            }
        }
    }
}
