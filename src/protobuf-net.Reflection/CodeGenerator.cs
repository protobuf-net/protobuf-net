using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProtoBuf.Reflection
{
    public abstract class CodeGenerator
    {
        public abstract string Name { get; }
        public override string ToString() => Name;

        public abstract IEnumerable<CodeFile> Generate(FileDescriptorSet set, NameNormalizer normalizer = null);
    }
    public abstract partial class CommonCodeGenerator : CodeGenerator
    {
        private Access? GetAccess(IType parent)
        {
            if (parent is DescriptorProto)
                return GetAccess((DescriptorProto)parent);
            if (parent is EnumDescriptorProto)
                return GetAccess((EnumDescriptorProto)parent);
            if (parent is FileDescriptorProto)
                return GetAccess((FileDescriptorProto)parent);
            return null;
        }
        protected Access GetAccess(FileDescriptorProto obj)
            => obj?.Options?.GetOptions()?.Access ?? Access.Public;

        static Access? NullIfInherit(Access? access)
            => access == Access.Inherit ? null : access;
        protected Access GetAccess(DescriptorProto obj)
            => NullIfInherit(obj?.Options?.GetOptions()?.Access)
            ?? GetAccess(obj?.Parent) ?? Access.Public;

        protected Access GetAccess(FieldDescriptorProto obj)
            => NullIfInherit(obj?.Options?.GetOptions()?.Access)
            ?? GetAccess(obj?.Parent as IType) ?? Access.Public;

        protected Access GetAccess(EnumDescriptorProto obj)
            => NullIfInherit(obj?.Options?.GetOptions()?.Access)
                ?? GetAccess(obj?.Parent) ?? Access.Public;

        public virtual string GetAccess(Access access)
            => access.ToString();
        

        public virtual string Indent => "    ";

        protected abstract string DefaultFileExtension { get; }

        protected abstract string Escape(string identifier);
        public override IEnumerable<CodeFile> Generate(FileDescriptorSet set, NameNormalizer normalizer = null)
        {
            foreach (var file in set.Files)
            {
                if (!file.IncludeInOutput) continue;

                var fileName = Path.ChangeExtension(file.Name, DefaultFileExtension);

                string generated;
                using (var buffer = new StringWriter())
                {
                    var ctx = new GeneratorContext(file, normalizer ?? NameNormalizer.Default, buffer, Indent);

                    ctx.BuildTypeIndex(); // populates for TryFind<T>
                    WriteFile(ctx, file);
                    generated = buffer.ToString();
                }
                yield return new CodeFile(fileName, generated);

            }

        }

        protected virtual void WriteFile(GeneratorContext ctx, FileDescriptorProto obj)
        {
            var file = ctx.File;
            object state = null;
            WriteFileHeader(ctx, obj, ref state);

            foreach (var inner in file.MessageTypes)
            {
                WriteMessage(ctx, inner);
            }
            foreach (var inner in file.EnumTypes)
            {
                WriteEnum(ctx, inner);
            }
            foreach (var inner in file.Services)
            {
                WriteService(ctx, inner);
            }
            if(file.Extensions.Count != 0)
            {
                object extState = null;
                WriteExtensionsHeader(ctx, file, ref extState);
                foreach(var ext in file.Extensions)
                {
                    WriteExtension(ctx, ext);
                }
                WriteExtensionsFooter(ctx, file, ref extState);
            }
            WriteFileFooter(ctx, obj, ref state);
        }

        protected virtual void WriteExtension(GeneratorContext ctx, FieldDescriptorProto ext) { }

        protected virtual void WriteExtensionsHeader(GeneratorContext ctx, FileDescriptorProto file, ref object state) { }
        protected virtual void WriteExtensionsFooter(GeneratorContext ctx, FileDescriptorProto file, ref object state) { }

        protected virtual void WriteExtensionsHeader(GeneratorContext ctx, DescriptorProto file, ref object state) { }
        protected virtual void WriteExtensionsFooter(GeneratorContext ctx, DescriptorProto file, ref object state) { }

        protected virtual void WriteService(GeneratorContext ctx, ServiceDescriptorProto obj)
        {
            object state = null;
            WriteServiceHeader(ctx, obj, ref state);
            foreach (var inner in obj.Methods)
            {
                WriteServiceMethod(ctx, inner, ref state);
            }
            WriteServiceFooter(ctx, obj, ref state);
        }

        protected virtual void WriteServiceFooter(GeneratorContext ctx, ServiceDescriptorProto obj, ref object state) { }

        protected virtual void WriteServiceMethod(GeneratorContext ctx, MethodDescriptorProto inner, ref object state) { }

        protected virtual void WriteServiceHeader(GeneratorContext ctx, ServiceDescriptorProto obj, ref object state) { }

        protected virtual bool ShouldOmitMessage(GeneratorContext ctx, DescriptorProto obj, ref object state)
            => obj.Options?.MapEntry ?? false; // don't write this type - use a dictionary instead
        protected virtual void WriteMessage(GeneratorContext ctx, DescriptorProto obj)
        {
            object state = null;
            if (ShouldOmitMessage(ctx, obj, ref state)) return;

            WriteMessageHeader(ctx, obj, ref state);
            var oneOfs = OneOfStub.Build(ctx, obj);
            foreach (var inner in obj.Fields)
            {
                WriteField(ctx, inner, ref state, oneOfs);
            }
            foreach (var inner in obj.NestedTypes)
            {
                WriteMessage(ctx, inner);
            }
            foreach (var inner in obj.EnumTypes)
            {
                WriteEnum(ctx, inner);
            }
            if (obj.Extensions.Count != 0)
            {
                object extState = null;
                WriteExtensionsHeader(ctx, obj, ref extState);
                foreach (var ext in obj.Extensions)
                {
                    WriteExtension(ctx, ext);
                }
                WriteExtensionsFooter(ctx, obj, ref extState);
            }
            WriteMessageFooter(ctx, obj, ref state);
        }

        protected abstract void WriteField(GeneratorContext ctx, FieldDescriptorProto obj, ref object state, OneOfStub[] oneOfs);
        protected abstract void WriteMessageFooter(GeneratorContext ctx, DescriptorProto obj, ref object state);

        protected abstract void WriteMessageHeader(GeneratorContext ctx, DescriptorProto obj, ref object state);

        protected virtual void WriteEnum(GeneratorContext ctx, EnumDescriptorProto obj)
        {
            object state = null;
            WriteEnumHeader(ctx, obj, ref state);
            foreach (var inner in obj.Values)
            {
                WriteEnumValue(ctx, inner, ref state);
            }
            WriteEnumFooter(ctx, obj, ref state);
        }

        protected abstract void WriteEnumHeader(GeneratorContext ctx, EnumDescriptorProto obj, ref object state);
        protected abstract void WriteEnumValue(GeneratorContext ctx, EnumValueDescriptorProto obj, ref object state);
        protected abstract void WriteEnumFooter(GeneratorContext ctx, EnumDescriptorProto obj, ref object state);

        protected virtual void WriteFileHeader(GeneratorContext ctx, FileDescriptorProto obj, ref object state) { }
        protected virtual void WriteFileFooter(GeneratorContext ctx, FileDescriptorProto obj, ref object state) { }


        protected class GeneratorContext
        {
            public FileDescriptorProto File { get; }
            public string IndentToken { get; }
            public int IndentLevel { get; private set; }
            public NameNormalizer NameNormalizer { get; }
            public TextWriter Output { get; }
            public string Syntax => string.IsNullOrWhiteSpace(File.Syntax) ? FileDescriptorProto.SyntaxProto2 : File.Syntax;

            public GeneratorContext(FileDescriptorProto file, NameNormalizer nameNormalizer, TextWriter output, string indentToken)
            {
                File = file;
                NameNormalizer = nameNormalizer;
                Output = output;
                IndentToken = indentToken;
            }

            public GeneratorContext WriteLine()
            {
                Output.WriteLine();
                return this;
            }
            public GeneratorContext WriteLine(string line)
            {
                var indentLevel = IndentLevel;
                var target = Output;
                while (indentLevel-- > 0)
                {
                    target.Write(IndentToken);
                }
                target.WriteLine(line);
                return this;
            }
            public TextWriter Write(string value)
            {
                var indentLevel = IndentLevel;
                var target = Output;
                while (indentLevel-- > 0)
                {
                    target.Write(IndentToken);
                }
                target.Write(value);
                return target;
            }

            public GeneratorContext Indent()
            {
                IndentLevel++;
                return this;
            }
            public GeneratorContext Outdent()
            {
                IndentLevel--;
                return this;
            }


            public T TryFind<T>(string typeName) where T : class
            {
                if (!_knownTypes.TryGetValue(typeName, out var obj) || obj == null)
                {
                    return null;
                }
                return obj as T;
            }

            private Dictionary<string, object> _knownTypes = new Dictionary<string, object>();

            internal void BuildTypeIndex()
            {
                void AddMessage(DescriptorProto message)
                {
                    _knownTypes[message.FullyQualifiedName] = message;
                    foreach (var @enum in message.EnumTypes)
                    {
                        _knownTypes[@enum.FullyQualifiedName] = @enum;
                    }
                    foreach (var msg in message.NestedTypes)
                    {
                        AddMessage(msg);
                    }
                }
                {
                    var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var pendingFiles = new Queue<FileDescriptorProto>();

                    _knownTypes.Clear();
                    processedFiles.Add(File.Name);
                    pendingFiles.Enqueue(File);

                    while (pendingFiles.Count != 0)
                    {
                        var file = pendingFiles.Dequeue();

                        foreach (var @enum in file.EnumTypes)
                        {
                            _knownTypes[@enum.FullyQualifiedName] = @enum;
                        }
                        foreach (var msg in file.MessageTypes)
                        {
                            AddMessage(msg);
                        }

                        if (file.HasImports())
                        {
                            foreach (var import in file.GetImports())
                            {
                                if (processedFiles.Add(import.Path))
                                {
                                    var importFile = file.Parent.GetFile(import.Path);
                                    if (importFile != null) pendingFiles.Enqueue(importFile);
                                }
                            }
                        }

                    }
                }
            }
        }
    }


}
