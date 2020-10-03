using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProtoBuf.Models
{
    public class GeneratorViewModel {
        private GeneratorLanguageEnum language;

        public enum GeneratorLanguageEnum {
            CSharp,
            CSharpProtoc,
            VBNet,
            CPlusPlus,
            Java,
            JS,
            Objc,
            PHP,
            Python,
            Ruby
        }
        public enum NamingConventionEnum {
            Auto,
            Original
        }
        private static Dictionary<GeneratorLanguageEnum, IEnumerable<string>> LanguageVersions { get; set; } = new Dictionary<GeneratorLanguageEnum, IEnumerable<string>> { { GeneratorLanguageEnum.CSharp, new [] { "7.1", "6", "3", "2" } },
            { GeneratorLanguageEnum.VBNet, new [] { "vb14", "vb11", "vb9" } }
        };

        [Required]
        public GeneratorLanguageEnum Language
        {
            get => language;
            set
            {
                language = value;
                LanguageVersion = null;
            }
        }

        public bool? Services { get; set; } = true;
        public bool? OneOfEnum { get; set; } = false;
        public bool? RepeatedEmitSetAccessors { get; set; } = false;

        public string LanguageVersion { get; set; }
        public NamingConventionEnum NamingConvention { get; set; } = NamingConventionEnum.Auto;

        public NameNormalizer GetNameNormalizerForConvention () {
            return NamingConvention switch
            {
                NamingConventionEnum.Auto => NameNormalizer.Default,
                NamingConventionEnum.Original => NameNormalizer.Null,
                _ => throw new ArgumentOutOfRangeException(nameof(NamingConvention)),
            };
        }

        public CodeGenerator GetCodeGenerator () {
            if (!IsProtogen ()) {
                throw new InvalidOperationException ("CodeGenerator are available only for language compatible with protobuf-net");
            }
            return Language switch
            {
                GeneratorLanguageEnum.CSharp => CSharpCodeGenerator.Default,
                GeneratorLanguageEnum.VBNet => VBCodeGenerator.Default,
                _ => throw new ArgumentOutOfRangeException($"{Language} is not supported"),
            };
        }

        public Dictionary<string, string> GetOptions () {
            var res = new Dictionary<string, string> ();
            if (LanguageVersion != null) {
                res.Add ("langver", LanguageVersion);
            }
            if (OneOfEnum.GetValueOrDefault (false)) {
                res.Add ("oneof", "enum");
            }
            if (Services.GetValueOrDefault(false))
            {
                res.Add("services", "yes");
            }
            if (RepeatedEmitSetAccessors.GetValueOrDefault (false)) {
                res.Add ("listset", "yes");
            }
            return res;
        }

        public bool IsProtogen () {
            return Language == GeneratorLanguageEnum.CSharp ||
                Language == GeneratorLanguageEnum.VBNet;
        }

        public string GetMonacoLanguage() => Language switch
        {   //taken from here https://github.com/microsoft/monaco-languages
            GeneratorLanguageEnum.VBNet => "vb",
            GeneratorLanguageEnum.CSharp => "csharp",
            GeneratorLanguageEnum.CSharpProtoc => "csharp",
            GeneratorLanguageEnum.CPlusPlus => "cpp",
            GeneratorLanguageEnum.Java => "java",
            GeneratorLanguageEnum.JS => "js",
            GeneratorLanguageEnum.Objc => "objective-c",
            GeneratorLanguageEnum.PHP => "php",
            GeneratorLanguageEnum.Python => "python",
            GeneratorLanguageEnum.Ruby => "ruby",
            _ => throw new ArgumentOutOfRangeException($"{Language} is not supported by Monaco"),
        };

        public string GetProtocTooling() => Language switch
        {
            GeneratorLanguageEnum.CSharpProtoc => "csharp",
            GeneratorLanguageEnum.CPlusPlus => "cpp",
            GeneratorLanguageEnum.Java => "java",
            GeneratorLanguageEnum.JS => "js",
            GeneratorLanguageEnum.Objc => "objc",
            GeneratorLanguageEnum.PHP => "php",
            GeneratorLanguageEnum.Python => "python",
            GeneratorLanguageEnum.Ruby => "ruby",
            _ => throw new ArgumentOutOfRangeException($"{Language} is not supported by protoc"),
        };

        [Required]
        public string ProtoContent { get; set; }

        public bool HasLanguageVersion () {
            return LanguageVersions.ContainsKey (Language);
        }
        public IEnumerable<string> GetLanguageVersions () {
            return LanguageVersions[Language];
        }
    }
}