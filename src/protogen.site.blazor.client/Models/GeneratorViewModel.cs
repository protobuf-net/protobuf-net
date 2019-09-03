using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ProtoBuf.Models
{
    public class GeneratorViewModel
    {
        private GeneratorLanguageEnum language;

        public enum GeneratorLanguageEnum
        {
            CSharp,
            VBNet
        }
        public enum NamingConventionEnum
        {
            Auto,
            Original
        }
        private static Dictionary<GeneratorLanguageEnum, IEnumerable<string>> LanguageVersions { get; set; } = new Dictionary<GeneratorLanguageEnum, IEnumerable<string>>
        {
            { GeneratorLanguageEnum.CSharp, new[] { "7.1", "6", "3", "2" }},
            { GeneratorLanguageEnum.VBNet, new[] { "vb14","vb11","vb9" }}
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
        public bool? OneOfEnum { get; set; } = false;
        public bool? RepeatedEmitSetAccessors { get; set; } = false;

        public bool SpecifyOptInProto { get; set; } = false;
        public string LanguageVersion { get; set; }
        public NamingConventionEnum NamingConvention { get; set; } = NamingConventionEnum.Auto;

        public NameNormalizer GetNameNormalizerForConvention()
        {
            switch (NamingConvention)
            {
                case NamingConventionEnum.Auto:
                    return NameNormalizer.Default;
                case NamingConventionEnum.Original:
                    return NameNormalizer.Null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(NamingConvention));
            }
        }

        public CodeGenerator GetCodeGenerator()
        {
            if (!IsProtobugGen())
            {
                throw new InvalidOperationException("CodeGenerator are available only for language compatible with protobuf-net");
            }
            switch (Language)
            {
                case GeneratorLanguageEnum.CSharp:
                    return CSharpCodeGenerator.Default;
                case GeneratorLanguageEnum.VBNet:
                    return VBCodeGenerator.Default;
                default:
                    throw new ArgumentOutOfRangeException($"{Language} is not supported");
            }
        }

        public Dictionary<string,string> GetOptions()
        {
            var res = new Dictionary<string, string>();
            if(LanguageVersion != null)
            {
                res.Add("langver", LanguageVersion);
            }
            if (OneOfEnum.GetValueOrDefault(false))
            {
                res.Add("oneof", "enum");
            }
            if (RepeatedEmitSetAccessors.GetValueOrDefault(false))
            {
                res.Add("listset", "yes");
            }
            return res;
        }

        public bool IsProtobugGen()
        {
            return Language == GeneratorLanguageEnum.CSharp ||
                Language == GeneratorLanguageEnum.VBNet;
        }

        [Required]
        public string ProtoContent { get; set; }

        public bool HasLanguageVersion()
        {
            return LanguageVersions.ContainsKey(Language);
        }
        public IEnumerable<string> GetLanguageVersions()
        {
            return LanguageVersions[Language];
        }
    }
}
