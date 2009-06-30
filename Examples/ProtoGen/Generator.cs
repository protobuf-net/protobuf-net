using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.CSharp;
using NUnit.Framework;
using ProtoBuf.CodeGenerator;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Text;
using ProtoBuf;

namespace Examples.ProtoGen
{
    [TestFixture]
    public class Generator
    {
        public static string GetCode(params string[] args)
        {

            // ensure we have quiet mode enabled
            if(Array.IndexOf(args, "-q") < 0)
            {
                Array.Resize(ref args, args.Length + 1);
                args[args.Length - 1] = "-q";
            }

            StringWriter sw = new StringWriter();
            CommandLineOptions.Parse(sw, args).Execute();
            return sw.ToString();

        }

        [Test]
        public void TestPersonAsCSharp()
        {
            string csharp = GetCode(@"-i:ProtoGen\person.proto", "-p:detectMissing");
            File.WriteAllText(@"ProtoGen\person.cs", csharp);
            TestCompileCSharp(csharp);
        }

        [Test]
        public void TestPersonAsVB()
        {
            string code = GetCode(@"-i:ProtoGen\person.proto", "-t:vb");
            File.WriteAllText(@"ProtoGen\person.vb", code);
            TestCompileVisualBasic(code);
        }

        [Test]
        public void TestPersonAsXml()
        {
            string csharp = GetCode(@"-i:ProtoGen\person.proto", "-t:xml");
            File.WriteAllText(@"ProtoGen\person.xml", csharp);
        }
        [Test]
        public void TestDescriptorAsXml()
        {
            string xml = GetCode(@"-i:ProtoGen\descriptor.proto", "-t:xml");
            TestLoadXml(xml);
        }

        [Test]
        public void TestDescriptorAsXmlToFile()
        {
            GetCode(@"-i:ProtoGen\descriptor.proto", "-o:descriptor.xml", "-t:xml");
            string viaFile = File.ReadAllText("descriptor.xml");

            TestLoadXml(viaFile);

            string viaWriter = GetCode(@"-i:ProtoGen\descriptor.proto", "-t:xml");
            Assert.AreEqual(viaFile, viaWriter);
        }


        [Test]
        public void TestDescriptorAsCSharpBasic()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto");
            TestCompileCSharp(code);
        }

        [Test]
        public void TestPersonAsCSharpCased()
        {
            string code = GetCode(@"-i:ProtoGen\person.proto", "-p:fixCase");
            File.WriteAllText(@"ProtoGen\personCased.cs", code);
            TestCompileCSharp(code);
        }

        [Test] //, Ignore("VB compiled, but problem with reference?")]
        public void TestDescriptorAsVB_Basic()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-t:vb");
            File.WriteAllText(@"ProtoGen\descriptor.vb", code);
            TestCompileVisualBasic(code);
        }

        [Test]
        public void TestDescriptorAsCSharpDetectMissing()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:detectMissing");
            TestCompileCSharp(code);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestDescriptorAsCSharpPartialMethodsLangVer2()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods");
            TestCompileCSharp(code);
        }

        [Test]
        public void TestDescriptorAsCSharpPartialMethodsLangVer3()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods");
            TestCompileCSharpV3(code);
        }

        [Test]
        public void TestDescriptorAsCSharpPartialMethodsLangVer3DetectMissing()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods", "-p:detectMissing");
            TestCompileCSharpV3(code);
        }

        [Test]
        public void TestDescriptorAsCSharpPartialMethodsLangVer3DetectMissingWithXml()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods", "-p:detectMissing", "-p:xml");
            TestCompileCSharpV3(code);
        }

        [Test]
        public void TestDescriptorAsCSharpPartialMethodsLangVer3DetectMissingWithDataContract()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods", "-p:detectMissing", "-p:datacontract");
            TestCompileCSharpV3(code, "System.Runtime.Serialization.dll");
        }


        private static void TestLoadXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            Assert.IsFalse(string.IsNullOrEmpty(doc.OuterXml), "xml should be non-empty");
        }

        public static void TestCompileCSharp(string code, params string[] extraReferences)
        {
            TestCompile<CSharpCodeProvider>(null, code, extraReferences);
        }
        public static void TestCompileVisualBasic(string code, params string[] extraReferences)
        {
            TestCompile<VBCodeProvider>(null, code, extraReferences);
        }
        private static void TestCompileCSharpV3(string code, params string[] extraReferences)
        {
            CSharpCodeProvider compiler = new CSharpCodeProvider(new Dictionary<string, string>()
            { { "CompilerVersion", "v3.5" } });
            TestCompile(compiler, code, extraReferences);
        }

        [Conditional("DEBUG")]
        static void DebugWriteAllText(string path, string contents)
        {
            File.WriteAllText(path,contents);
        }

        private static void TestCompile<T>(T compiler, string code, params string[] extraReferences)
            where T : CodeDomProvider
        {
            if (compiler == null) compiler = (T) Activator.CreateInstance(typeof (T));
            string path = Path.GetTempFileName();
            try
            {
                List<string> refs = new List<string> {
                    typeof(Uri).Assembly.Location,
                    typeof(XmlDocument).Assembly.Location,
                    typeof(Serializer).Assembly.Location
                };
                if(extraReferences != null && extraReferences.Length > 0)
                {
                    refs.AddRange(extraReferences);
                }
                CompilerParameters args = new CompilerParameters(refs.ToArray(), path, false);
                CompilerResults results = compiler.CompileAssemblyFromSource(args, code);
                DebugWriteAllText(Path.ChangeExtension("last.cs", compiler.FileExtension), code);
                ShowErrors(results.Errors);
                if(results.Errors.Count > 0)
                {
                    foreach (CompilerError err in results.Errors)
                    {
                        Console.Error.WriteLine(err);
                    }
                    throw new InvalidOperationException(
                        string.Format("{0} found {1} code errors errors",
                            typeof(T).Name, results.Errors.Count));
                }
                
            }
            finally
            {
                try { File.Delete(path); } catch {} // best effort
            }
        }
        static void ShowErrors(CompilerErrorCollection errors)
        {
            if (errors.Count > 0)
            {
                foreach (CompilerError err in errors)
                {
                    Console.Error.Write(err.IsWarning ? "Warning: " : "Error: ");
                    Console.Error.WriteLine(err.ErrorText);
                }
            }
        }
    }
}
