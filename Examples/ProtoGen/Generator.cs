using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.CSharp;
using NUnit.Framework;
using ProtoBuf.CodeGenerator;

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

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestDescriptorAsCSharpPartialMethodsLangVer2()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods");
            TestCompileCSharp(code);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestDescriptorAsCSharpPartialMethodsLangVer3()
        {
            string code = GetCode(@"-i:ProtoGen\descriptor.proto", "-p:partialMethods");
            TestCompileCSharpV3(code);
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
        private static void TestCompileCSharpV3(string code, params string[] extraReferences)
        {
            CSharpCodeProvider compiler = new CSharpCodeProvider();
            TestCompile(compiler, code, extraReferences);
        }
        private static void TestCompile<T>(T compiler, string code, params string[] extraReferences)
            where T : CodeDomProvider
        {
            if (compiler == null) compiler = (T) Activator.CreateInstance(typeof (T));
            string path = Path.GetTempFileName();
            try
            {
                List<string> refs = new List<string> { "System.dll", "System.Xml.dll", "protobuf-net.dll"};
                if(extraReferences != null && extraReferences.Length > 0)
                {
                    refs.AddRange(extraReferences);
                }
                CompilerParameters args = new CompilerParameters(refs.ToArray(), path, false);
                CompilerResults results = compiler.CompileAssemblyFromSource(args, code);
                ShowErrors(results.Errors);
                if(results.Errors.Count > 0)
                {
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
