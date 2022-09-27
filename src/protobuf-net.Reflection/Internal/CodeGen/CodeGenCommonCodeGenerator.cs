using ProtoBuf.Reflection.Internal.CodeGen;
using ProtoBuf.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System;
using System.Linq;

namespace ProtoBuf.Reflection.Internal.CodeGen;

/// <summary>
/// Abstract base class for a code generator that uses a visitor pattern
/// </summary>
internal abstract partial class CodeGenCommonCodeGenerator : CodeGenCodeGenerator
{
    /// <summary>
    /// The file extension of the files generatred by this generator
    /// </summary>
    protected abstract string DefaultFileExtension { get; }

    /// <summary>
    /// Should case-sensitivity be used when computing conflicts?
    /// </summary>
    protected internal virtual bool IsCaseSensitive => true;

    /// <summary>
    /// The indentation used by this code generator
    /// </summary>
    public virtual string Indent => "    ";

    /// <summary>
    /// Handle keyword escaping in the language of this code generator
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    protected abstract string Escape(string identifier);

    /// <summary>
    /// Get the textual name of a given access level
    /// </summary>
    public virtual string GetAccess(Access access)
        => access.ToString();

    /// <summary>
    /// Execute the code generator against a FileDescriptorSet, yielding a sequence of files
    /// </summary>
    public override IEnumerable<CodeFile> Generate(CodeGenSet set, Dictionary<string, string> options = null)
    {
        foreach (var file in set.Files)
        { 
            var fileName = Path.ChangeExtension(file.Name, DefaultFileExtension);

            string generated;
            using (var buffer = new StringWriter())
            {
                var ctx = new CodeGenGeneratorContext(this, file, buffer, Indent, options);

                WriteFile(ctx, file);
                generated = buffer.ToString();
            }
            yield return new CodeFile(fileName, generated);
        }
    }

    /// <summary>
    /// Emits the code for a file in a descriptor-set
    /// </summary>
    protected virtual void WriteFile(CodeGenGeneratorContext ctx, CodeGenFile file)
    {
        object state = null;
        WriteFileHeader(ctx, file, ref state);

        //var @namespace = ctx.NameNormalizer.GetName(file) ?? "";

        //if (!string.IsNullOrWhiteSpace(@namespace))
        //    WriteNamespaceHeader(ctx, @namespace);

        var messagesByNamespace = file.Messages.ToLookup(x => x.FullyQualifiedPrefix);
        var enumsByNamespace = file.Enums.ToLookup(x => x.FullyQualifiedPrefix);
        var servicesByNamespace = file.Services.ToLookup(x => x.FullyQualifiedPrefix);
        var namespaces = messagesByNamespace.Select(x => x.Key).Union(enumsByNamespace.Select(x => x.Key));

        void WriteMessagesAndEnums(string grp)
        {
            foreach (var inner in messagesByNamespace[grp])
            {
                WriteMessage(ctx, inner);
            }
            foreach (var inner in enumsByNamespace[grp])
            {
                WriteEnum(ctx, inner);
            }
            foreach (var inner in servicesByNamespace[grp])
            {
                WriteService(ctx, inner);
            }
        }

        //WriteMessagesAndEnums(@namespace);

        //foreach (var inner in file.Services)
        //{
        //    WriteService(ctx, inner);
        //}
        //if (file.Extensions.Count != 0)
        //{
        //    object extState = null;
        //    WriteExtensionsHeader(ctx, file, ref extState);
        //    foreach (var ext in file.Extensions)
        //    {
        //        WriteExtension(ctx, ext);
        //    }
        //    WriteExtensionsFooter(ctx, file, ref extState);
        //}

        //if (!string.IsNullOrWhiteSpace(@namespace))
        //    WriteNamespaceFooter(ctx, @namespace);


        foreach (var altNs in namespaces)
        {
            // if (altNs == @namespace) continue;
            WriteNamespaceHeader(ctx, altNs);
            WriteMessagesAndEnums(altNs);
            WriteNamespaceFooter(ctx, altNs);
        }

        WriteFileFooter(ctx, file, ref state);
    }

    /// <summary>
    /// Opens the stated namespace
    /// </summary>
    protected abstract void WriteNamespaceHeader(CodeGenGeneratorContext ctx, string @namespace);
    /// <summary>
    /// Closes the stated namespace
    /// </summary>
    protected abstract void WriteNamespaceFooter(CodeGenGeneratorContext ctx, string @namespace);

    /// <summary>
    /// Emit code representing an extension field
    /// </summary>
    protected virtual void WriteExtension(CodeGenGeneratorContext ctx, CodeGenField field) { }
    /// <summary>
    /// Emit code preceeding a set of extension fields
    /// </summary>
    protected virtual void WriteExtensionsHeader(CodeGenGeneratorContext ctx, CodeGenFile file, ref object state) { }
    /// <summary>
    /// Emit code following a set of extension fields
    /// </summary>
    protected virtual void WriteExtensionsFooter(CodeGenGeneratorContext ctx, CodeGenFile file, ref object state) { }
    /// <summary>
    /// Emit code preceeding a set of extension fields
    /// </summary>
    protected virtual void WriteExtensionsHeader(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state) { }
    /// <summary>
    /// Emit code following a set of extension fields
    /// </summary>
    protected virtual void WriteExtensionsFooter(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state) { }
    ///// <summary>
    ///// Emit code representing a service
    ///// </summary>
    //protected virtual void WriteService(CodeGenGeneratorContext ctx, ServiceDescriptorProto service)
    //{
    //    if (ctx.EmitServices)
    //    {
    //        object state = null;
    //        WriteServiceHeader(ctx, service, ref state);
    //        foreach (var inner in service.Methods)
    //        {
    //            WriteServiceMethod(ctx, inner, ref state);
    //        }
    //        WriteServiceFooter(ctx, service, ref state);
    //    }
    //}
    ///// <summary>
    ///// Emit code following a set of service methods
    ///// </summary>
    //protected virtual void WriteServiceFooter(CodeGenGeneratorContext ctx, ServiceDescriptorProto service, ref object state) { }

    ///// <summary>
    ///// Emit code representing a service method
    ///// </summary>
    //protected virtual void WriteServiceMethod(CodeGenGeneratorContext ctx, MethodDescriptorProto method, ref object state) { }
    ///// <summary>
    ///// Emit code preceeding a set of service methods
    ///// </summary>
    //protected virtual void WriteServiceHeader(CodeGenGeneratorContext ctx, ServiceDescriptorProto service, ref object state) { }

    /// <summary>
    /// Emit code representing a message type
    /// </summary>
    protected virtual void WriteMessage(CodeGenGeneratorContext ctx, CodeGenMessage message)
    {
        object state = null;

        WriteMessageHeader(ctx, message, ref state);
        //var oneOfs = OneOfStub.Build(message);

        if (WriteContructorHeader(ctx, message, ref state))
        {
            foreach (var inner in message.Fields)
            {
                WriteInitField(ctx, inner, ref state);
            }
            WriteConstructorFooter(ctx, message, ref state);
        }
        int maxFP = -1;
        foreach (var inner in message.Fields)
        {
            if (inner.Conditional == ConditionalKind.FieldPresence && inner.FieldPresenceIndex > maxFP) maxFP = inner.FieldPresenceIndex;
        }
        if (maxFP >= 0)
        {
            WriteFieldPresence(ctx, maxFP);
        }
        foreach (var inner in message.Fields)
        {
            WriteField(ctx, inner, ref state);
        }

        //if (oneOfs != null)
        //{
        //    foreach (var stub in oneOfs)
        //    {
        //        WriteOneOf(ctx, stub);
        //    }
        //}

        foreach (var inner in message.Messages)
        {
            WriteMessage(ctx, inner);
        }
        foreach (var inner in message.Enums)
        {
            WriteEnum(ctx, inner);
        }
        //if (message.Extensions.Count != 0)
        //{
        //    object extState = null;
        //    WriteExtensionsHeader(ctx, message, ref extState);
        //    foreach (var ext in message.Extensions)
        //    {
        //        WriteExtension(ctx, ext);
        //    }
        //    WriteExtensionsFooter(ctx, message, ref extState);
        //}
        WriteMessageSerializer(ctx, message, ref state);
        WriteMessageFooter(ctx, message, ref state);
    }

    /// <summary>
    /// Emit code terminating a constructor, if one is required
    /// </summary>
    protected virtual void WriteConstructorFooter(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state) { }

    /// <summary>
    /// Emit code initializing field values inside a constructor, if one is required
    /// </summary>
    protected virtual void WriteInitField(CodeGenGeneratorContext ctx, CodeGenField field, ref object state) { }

    /// <summary>
    /// Emit code beginning a constructor, if one is required
    /// </summary>
    /// <returns>true if a constructor is required</returns>
    protected virtual bool WriteContructorHeader(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state) => false;

    /// <summary>
    /// Emit code representing a message field
    /// </summary>
    protected abstract void WriteField(CodeGenGeneratorContext ctx, CodeGenField field, ref object state);

    /// <summary>
    /// Emit code defining field-presence tracking fields
    /// </summary>
    protected abstract void WriteFieldPresence(CodeGenGeneratorContext ctx, int maxFieldPresenceIndex);

    /// <summary>
    /// Emit code following a set of message fields
    /// </summary>
    protected abstract void WriteMessageFooter(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state);

    protected virtual void WriteMessageSerializer(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state)
    {
        // no implemnetation by default
    }

    /// <summary>
    /// Emit code preceeding a set of message fields
    /// </summary>
    protected abstract void WriteMessageHeader(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state);

    /// <summary>
    /// Emit code representing an enum type
    /// </summary>
    protected virtual void WriteEnum(CodeGenGeneratorContext ctx, CodeGenEnum obj)
    {
        object state = null;
        WriteEnumHeader(ctx, obj, ref state);
        foreach (var enumValue in obj.EnumValues)
        {
            WriteEnumValue(ctx, enumValue, ref state);
        }
        // WriteEnumSerializer(...);
        WriteEnumFooter(ctx, obj, ref state);
    }
    
    /// <summary>
    /// Emit code representing an service type
    /// </summary>
    protected virtual void WriteService(CodeGenGeneratorContext ctx, CodeGenService obj)
    {
        object state = null;
        WriteServiceHeader(ctx, obj, ref state);
        foreach (var serviceMethod in obj.ServiceMethods)
        {
            WriteServiceMethod(ctx, serviceMethod, ref state);
        }
        // WriteServiceSerializer(...);
        WriteServiceFooter(ctx, obj, ref state);
    }

    ///// <summary>
    ///// Emit code representing 'oneof' elements as an enum discriminator
    ///// </summary>
    //protected virtual void WriteOneOf(GeneratorContext ctx, OneOfStub stub)
    //{
    //    if (ctx.OneOfEnums)
    //    {
    //        int index = stub.Index;
    //        var obj = stub.OneOf;
    //        object state = null;
    //        WriteOneOfDiscriminator(ctx, obj, ref state);

    //        WriteOneOfEnumHeader(ctx, obj, ref state);
    //        foreach (var field in obj.Parent.Fields)
    //        {
    //            if (field.ShouldSerializeOneofIndex() && field.OneofIndex == index)
    //            {
    //                WriteOneOfEnumValue(ctx, field, ref state);
    //            }
    //        }
    //        WriteOneOfEnumFooter(ctx, obj, ref state);
    //    }
    //}

    /// <summary>
    /// Emit code preceeding a set of enum values
    /// </summary>
    protected abstract void WriteEnumHeader(CodeGenGeneratorContext ctx, CodeGenEnum @enum, ref object state);

    /// <summary>
    /// Emit code representing an enum value
    /// </summary>
    protected abstract void WriteEnumValue(CodeGenGeneratorContext ctx, CodeGenEnumValue enumValue, ref object state);

    /// <summary>
    /// Emit code following a set of enum values
    /// </summary>
    protected abstract void WriteEnumFooter(CodeGenGeneratorContext ctx, CodeGenEnum @enum, ref object state);
    
    /// <summary>
    /// Emit code preceding a set of service methods
    /// </summary>
    protected abstract void WriteServiceHeader(CodeGenGeneratorContext ctx, CodeGenService service, ref object state);
    
    /// <summary>
    /// Emit code representing a single service method
    /// </summary>
    protected abstract void WriteServiceMethod(CodeGenGeneratorContext ctx, CodeGenServiceMethod method, ref object state);
    
    /// <summary>
    /// Emit code following service methods
    /// </summary>
    protected abstract void WriteServiceFooter(CodeGenGeneratorContext ctx, CodeGenService service, ref object state);

    /// <summary>
    /// Emit code at the start of a file
    /// </summary>
    protected virtual void WriteFileHeader(CodeGenGeneratorContext ctx, CodeGenFile file, ref object state) { }

    /// <summary>
    /// Emit code at the end of a file
    /// </summary>
    protected virtual void WriteFileFooter(CodeGenGeneratorContext ctx, CodeGenFile file, ref object state) { }

    ///// <summary>
    ///// Emit the start of an enum declaration for 'oneof' groups, including the 0/None element
    ///// </summary>
    //protected virtual void WriteOneOfEnumHeader(GeneratorContext ctx, OneofDescriptorProto oneof, ref object state) { }

    ///// <summary>
    ///// Emit a field-based entry for a 'oneof' groups's enum
    ///// </summary>
    //protected virtual void WriteOneOfEnumValue(GeneratorContext ctx, FieldDescriptorProto field, ref object state) { }

    ///// <summary>
    ///// Emit the end of an enum declaration for 'oneof' groups
    ///// </summary>
    //protected virtual void WriteOneOfEnumFooter(GeneratorContext ctx, OneofDescriptorProto oneof, ref object state) { }

    ///// <summary>
    ///// Emit  the discriminator accessor for 'oneof' groups
    ///// </summary>
    //protected virtual void WriteOneOfDiscriminator(GeneratorContext ctx, OneofDescriptorProto oneof, ref object state) { }

    /// <summary>
    /// Convention-based suffix for 'oneof' enums
    /// </summary>
    protected const string OneOfEnumSuffixEnum = "OneofCase";

    /// <summary>
    /// Convention-based suffix for 'oneof' discriminators
    /// </summary>

    protected const string OneOfEnumSuffixDiscriminator = "Case";

    /// <summary>
    /// Indicates the kinds of service metadata that should be included
    /// </summary>
    [Flags]
    protected enum ServiceKinds
    {
        /// <summary>
        /// No serivices should be included
        /// </summary>
        None = 0,
        /// <summary>
        /// Indicates service metadata defined by WCF (System.ServiceModel) should be included
        /// </summary>
        Wcf = 1 << 0,
        /// <summary>
        /// Indicates service metadata defined by protobuf-net.Grpc should be included
        /// </summary>
        Grpc = 1 << 1,
    }

    /// <summary>
    /// Represents the state of a code-generation invocation
    /// </summary>
    protected class CodeGenGeneratorContext
    {
        /// <summary>
        /// The file being processed
        /// </summary>
        public CodeGenFile File { get; }
        /// <summary>
        /// The token to use for indentation
        /// </summary>
        public string IndentToken { get; }
        /// <summary>
        /// The current indent level
        /// </summary>
        public int IndentLevel { get; private set; }

        /// <summary>
        /// The output for this code generation
        /// </summary>
        public TextWriter Output { get; }

        /// <summary>
        /// Whether to emit enums and discriminators for oneof groups
        /// </summary>
        internal bool OneOfEnums { get; }

        /// <summary>
        /// Create a new GeneratorContext instance
        /// </summary>
        internal CodeGenGeneratorContext(CodeGenCommonCodeGenerator generator, CodeGenFile file, TextWriter output, string indentToken, Dictionary<string, string> options)
        {
            string langver = null;
            if (options != null) options.TryGetValue("langver", out langver); // explicit option first
            if (string.IsNullOrWhiteSpace(langver)) langver = generator?.GetLanguageVersion(file); // then from file

            File = file;
            Output = output;
            IndentToken = indentToken;

            LanguageVersion = ParseVersion(langver);
            EmitRequiredDefaults = false; // file.Options.GetOptions()?.EmitRequiredDefaults ?? false;
            _options = options;

            OneOfEnums = false; // (File.Options?.GetOptions()?.EmitOneOfEnum ?? false) || (_options != null && _options.TryGetValue("oneof", out var oneof) && string.Equals(oneof, "enum", StringComparison.OrdinalIgnoreCase));

            EmitListSetters = IsEnabled("listset");

            var s = GetCustomOption("services");
            void AddServices(string value)
            {
                value = value?.Trim();
                if (string.IsNullOrWhiteSpace(value)) return;

                if (!Enum.TryParse(value, true, out ServiceKinds parsed))
                {   // for backwards-compatibility of what "services" meant in the past
                    parsed = IsEnabledValue(value) ? ServiceKinds.Wcf : ServiceKinds.None;
                }
                _serviceKinds |= parsed;
            }
            if (s is not null && s.IndexOf(';') >= 0)
            {
                foreach (var part in s.Split(';'))
                {
                    AddServices(part);
                }
            }
            else
            {
                AddServices(s);
            }
        }

        private ServiceKinds _serviceKinds;

        /// <summary>
        /// Whether lists should be written with getters
        /// </summary>
        public bool EmitListSetters { get; }

        /// <summary>
        /// Whether services should be emitted
        /// </summary>
        public bool EmitServices => _serviceKinds != ServiceKinds.None;


        /// <summary>
        /// What kinds of services should be emitted
        /// </summary>
        public bool EmitServicesFor(ServiceKinds anyOf)
            => (_serviceKinds & anyOf) != 0;

        /// <summary>
        /// Whether a custom option is enabled
        /// </summary>
        internal bool IsEnabled(string key)
            => IsEnabledValue(GetCustomOption(key));

        internal bool IsEnabledValue(string option)
        {
            if (string.IsNullOrWhiteSpace(option)) return false;
            option = option.Trim();
            if (option == "1") return true;
            if (string.Equals("yes", option, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals("true", option, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals("on", option, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private readonly Dictionary<string, string> _options;
        /// <summary>
        /// Gets the value of an OPTION/VALUE pair provided to the system
        /// </summary>
        public string GetCustomOption(string key)
        {
            string value = null;
            _options?.TryGetValue(key, out value);
            return value;
        }

        private static Version ParseVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return null;
            version = version.Trim();

            if (Version.TryParse(version, out Version v)) return v;

            if (int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return new Version(i, 0);

            return null;
        }

        /// <summary>
        /// Should default value initializers be emitted even for required values?
        /// </summary>
        internal bool EmitRequiredDefaults { get; set; }

        internal bool Supports(Version version)
        {
            if (version == null) return true;
            var langver = LanguageVersion;
            if (langver == null) return true; // default is highest
            return langver >= version;
        }

        /// <summary>
        /// The specified language version (null if not specified)
        /// </summary>
        public Version LanguageVersion { get; }
        public bool Strict { get; set; }

        /// <summary>
        /// Ends the current line
        /// </summary>
        public CodeGenGeneratorContext WriteLine()
        {
            Output.WriteLine();
            return this;
        }
        /// <summary>
        /// Appends a value and ends the current line
        /// </summary>
        public CodeGenGeneratorContext WriteLine(string line)
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
        /// <summary>
        /// Appends a value to the current line
        /// </summary>
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
        /// <summary>
        /// Increases the indentation level
        /// </summary>
        public CodeGenGeneratorContext Indent()
        {
            IndentLevel++;
            return this;
        }
        /// <summary>
        /// Decreases the indentation level
        /// </summary>
        public CodeGenGeneratorContext Outdent()
        {
            IndentLevel--;
            return this;
        }
    }

    /// <summary>
    /// Get the language version for this language from a schema
    /// </summary>
    protected virtual string GetLanguageVersion(CodeGenFile obj) => null;
}