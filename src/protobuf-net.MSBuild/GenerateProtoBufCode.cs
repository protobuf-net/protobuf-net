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

        public string DefaultNamespace { get; set; }

        public string Language { get; set; }

        [Output]
        public ITaskItem[] ProtoCodeFile { get; set; }

        static CommonCodeGenerator GetCodeGenForLanguage(string lang)
        {
            if(lang == null)
                return CSharpCodeGenerator.Default;

            switch (lang)
            {
                case "C#":
                    return CSharpCodeGenerator.Default;
                case "VB":
                    return VBCodeGenerator.Default;
                default:
                    throw new NotSupportedException("protobuf code generation is not supported for language " + lang);
            }
        }

        public override bool Execute()
        {
            var codegen = GetCodeGenForLanguage(Language);

            if (ProtoDef == null || ProtoDef.Length == 0)
            {
                return true;
            }

            var set = new FileDescriptorSet
            {
                DefaultPackage = DefaultNamespace
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

            var errors = set.GetErrors();
            if(errors != null && errors.Length > 0)
            {
                foreach(var error in errors)
                {
                    if (error.IsError)
                    {
                        this.Log.LogError(null, null, null, error.File, error.LineNumber, error.ColumnNumber, 0, 0, error.Message);
                    }
                    else if(error.IsWarning)
                    {
                        this.Log.LogWarning(null, null, null, error.File, error.LineNumber, error.ColumnNumber, 0, 0, error.Message);
                    }
                }
                return false;
            }

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
