using System;
using System.Diagnostics;
using Xunit;
using System.IO;
using System.Text;

namespace Examples
{
    /// <summary>
    /// Runs <c>PEVerify.exe</c> over a persisted model assembly. net472 only — the persisted-dll
    /// path itself is (<c>PLAT_NO_EMITDLL</c>), so no AOT or modern-TFM run reaches this.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Gap B12.</b> This was the prime suspect for an intermittent full-traversal failure seen
    /// twice and never reproduced standalone: a subprocess with a fixed 20-second budget is exactly
    /// what contention in a parallel run trips. Two things made it impossible to confirm, and both
    /// are fixed here rather than the timeout alone:
    /// </para>
    /// <list type="bullet">
    /// <item><description>the child's output was never captured, so a genuine verification failure
    /// reported only "expected 0, actual 1" and named nothing;</description></item>
    /// <item><description>a timeout and a failure were indistinguishable in a log after the
    /// fact.</description></item>
    /// </list>
    /// <para>
    /// So: the budget is generous and overridable, the output is captured and reported, and a
    /// timeout says plainly that it is a harness/contention symptom rather than invalid IL. If B12
    /// recurs it should now arrive self-describing.
    /// </para>
    /// </remarks>
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

        /// <summary>
        /// How long to allow, in seconds. Generous by default because the cost of being wrong is an
        /// unreproducible red build, not a slow one; a passing run exits in well under a second.
        /// </summary>
        private static int TimeoutSeconds
            => int.TryParse(Environment.GetEnvironmentVariable("PBN_PEVERIFY_TIMEOUT"), out var seconds)
                && seconds > 0 ? seconds : 120;
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
            var psi = new ProcessStartInfo(exePath, "\"" + path + "\"")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var output = new StringBuilder();
            var timeout = TimeoutSeconds;
            using (var proc = Process.Start(psi))
            {
                // read asynchronously: a child that fills its pipe while we block on WaitForExit
                // deadlocks, and PEVerify is chatty when it disagrees
                proc.OutputDataReceived += (_, e) => { if (e.Data is object) lock (output) output.AppendLine(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (e.Data is object) lock (output) output.AppendLine(e.Data); };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                if (!proc.WaitForExit(timeout * 1000))
                {
                    try { proc.Kill(); } catch { }
                    throw new TimeoutException(
                        "PEVerify did not finish within " + timeout + "s for " + path
                        + ". That is a harness/contention symptom rather than invalid IL - see gap B12 -"
                        + " and PBN_PEVERIFY_TIMEOUT raises the budget.");
                }
                string captured;
                lock (output) captured = output.ToString();
                Assert.True(proc.ExitCode == 0,
                    "PEVerify reported invalid IL in " + path + " (exit code " + proc.ExitCode + "):"
                    + Environment.NewLine + captured);
            }
#endif
        }
    }
}
