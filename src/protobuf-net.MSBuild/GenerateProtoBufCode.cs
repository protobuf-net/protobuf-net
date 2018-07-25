using Google.Protobuf.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtoBuf.MSBuild
{
    public class GenerateProtoBufCode : Task
    {
        public ITaskItem[] ProtoDef { get; set; }


        public ITaskItem[] ImportPaths { get; set; }

        public string OutputPath { get; set; }

        [Output]
        public ITaskItem[] ProtoCodeFile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Dir: " + Environment.CurrentDirectory);
            var codegen = CSharpCodeGenerator.Default;

            if (ProtoDef == null || ProtoDef.Length == 0)
            {
                Log.LogMessage("Nothing to process");
                return true;
            } 

            var set = new FileDescriptorSet
            {
                DefaultPackage = "test" // not sure what this is? ns maybe?
            };

            if (ImportPaths == null || ImportPaths.Length == 0)
            {
                set.AddImportPath(Directory.GetCurrentDirectory());
            }
            else
            {
                foreach (var dir in ImportPaths)
                {
                    if (Directory.Exists(dir.ItemSpec))
                    {
                        set.AddImportPath(dir.ItemSpec);
                    }
                    else
                    {
                        this.Log.LogError($"Directory not found: {dir}");
                        return false;
                    }
                }
            }
            foreach (var input in ProtoDef)
            {
                if (!set.Add(input.ItemSpec, true))
                {
                    Log.LogError($"File not found: {input}");
                    return false;
                }
            }

            set.Process();
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //options[lhs.Substring(1)] = rhs;

            var codeFiles = new List<ITaskItem>();
            var files = codegen.Generate(set, options: options);
            foreach (var file in files)
            {
                var path = Path.Combine(OutputPath, file.Name);
                var dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);
                File.WriteAllText(path, file.Text);
                codeFiles.Add(new TaskItem(path));
            }

            this.ProtoCodeFile = codeFiles.Cast<ITaskItem>().ToArray();

            return true;
        }
    }
}
