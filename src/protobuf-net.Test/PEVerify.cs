using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xunit;
using System.Diagnostics;
using ProtoBuf.Meta;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoBuf.unittest
{
    static class PEVerify
    {
#if COREFX && !NET9_0_OR_GREATER
#pragma warning disable IDE0060 // unused params; the idea here is to make the API similar
        public static ProtoBuf.Meta.TypeModel Compile(this ProtoBuf.Meta.RuntimeTypeModel model, string name, string path)
#pragma warning restore IDE0060 
        {
            // dummy to avoid lots of test hackery for dll compilation tests
            return model.Compile();
        }
        internal static TypeModel CompileAndVerify(this RuntimeTypeModel model,
            [CallerMemberName] string name = null, int exitCode = 0, bool deleteOnSuccess = true, bool forceLongBranches = false)
        {
            var options = new RuntimeTypeModel.CompilerOptions { ForceLongBranches = forceLongBranches };
            return model.Compile(options);
        }
#else
        
        static readonly bool unavailable = true;
        static readonly string exePath = "";

#if COREFX
        static PEVerify()
        {
            try
            {
                using var proc = Process.Start("ilverify", "--version");
                if (proc.WaitForExit(2000))
                {
                    // unavailable = proc.ExitCode != 0; // TODO: finish this
                    exePath = "ilverify";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
#else
        static PEVerify()
        {
            exePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\PEVerify.exe");
            unavailable = !File.Exists(exePath);
            if (unavailable)
            {
                Console.Error.WriteLine("PEVerify not found at " + exePath);
            }
        }
#endif
        
        internal static TypeModel CompileAndVerify(this RuntimeTypeModel model,
            [CallerMemberName] string name = null, int exitCode = 0, bool deleteOnSuccess = true, bool forceLongBranches = false)
        {
            name = $"{name}_{Interlocked.Increment(ref index)}";
            var path = Path.ChangeExtension(name, "dll");
            if (File.Exists(path)) File.Delete(path);
            
            var options = new RuntimeTypeModel.CompilerOptions()
            {
                TypeName = name,
#pragma warning disable CS0618
                OutputPath = path,
                ForceLongBranches = true,
#pragma warning restore CS0618
            };
            var compiled = model.Compile(options);
            Verify(path, exitCode, deleteOnSuccess);
            return compiled;
        }
#endif

#if NETFRAMEWORK || NET9_0_OR_GREATER
        private static int index = 0;
#endif
        
        public static void Verify(string path, int exitCode = 0, bool deleteOnSuccess = true)
        {
#if NETFRAMEWORK || NET9_0_OR_GREATER
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            if (new FileInfo(path).Length == 0)
            {
                throw new InvalidOperationException($"File is empty: {path}");
            }
            
            if (unavailable)
            {
                if (deleteOnSuccess) try { File.Delete(path); } catch { }
                return;
            }
            ProcessStartInfo psi = new ProcessStartInfo(exePath, path)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using Process proc = Process.Start(psi);
            if (proc.WaitForExit(20000))
            {
                Assert.Equal(exitCode, proc.ExitCode); //, path);
                if (deleteOnSuccess) try { File.Delete(path); } catch { }
            }
            else
            {
                try { proc.Kill(); } catch { }
                throw new TimeoutException("PEVerify " + path);
            }
#endif
        }
    }
}
