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
        public bool OneOfEnum { get; set; } = false;
        public bool RepeatedEmitSetAccessors { get; set; } = false;
        public string LanguageVersion { get; set; }
        public NamingConventionEnum NamingConvention { get; set; } = NamingConventionEnum.Auto;

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
