using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.CodeGenerator
{
    static class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Main(string[] args)
        {
            try
            {
                CommandLineOptions.Parse(Console.Out,args).Execute();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                return 1;
            }
        }

        //private static void TestCompile(GenerationOptions options, string code) {
        //    CompilerResults results;
        //    switch(options.Template) {
        //        case GenerationOptions.TEMPLATE_CSHARP:
        //            {
        //                CSharpCodeProvider csc = new CSharpCodeProvider();
        //                string[] refs = new string[] { "System.dll", "System.Xml.dll", "protobuf-net.dll" };
        //                CompilerParameters cscArgs = new CompilerParameters(refs, "descriptor.dll", false);
        //                results = csc.CompileAssemblyFromSource(cscArgs, code);
        //                break;
        //            }
        //        default:
        //            Console.Error.WriteLine("No compiler available to test code with template " + options.Template);
        //            return;
        //    }
        //    ShowErrors(results.Errors);
        //}

        //static void ShowErrors(CompilerErrorCollection errors)
        //{
        //    if (errors.Count > 0)
        //    {
        //        Console.Error.Write(errors.Count + " errors");
        //        foreach (CompilerError err in errors)
        //        {
        //            Console.Error.Write(err.IsWarning ? "Warning: " : "Error: ");
        //            Console.Error.WriteLine(err.ErrorText);
        //        }
        //    }
        //}

        

        


        
    }
}
