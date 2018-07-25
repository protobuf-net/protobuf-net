using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtoBuf
{
    // if the namespace hasn't been explicitly assigned, will determine based
    // on the NamespaceRoot and relative file path.
    public class DetermineNamespace : Task
    {
        public string NamespaceRoot { get; set; }

        public ITaskItem[] Items { get; set; }

        [Output]
        public ITaskItem[] Output { get; set; }

        const string Separator = ".";
        const string NamespaceMetadata = "Namespace";
        static readonly char[] PathSeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        static string ToIdentifier(string str)
        {
            str = Regex.Replace(str, "\\s+", "");
            return str;
        }

        static string Combine(params string[] parts)
        {
            var sb = new StringBuilder();

            foreach (var part in parts)
            {
                var id = ToIdentifier(part);

                if (!string.IsNullOrEmpty(id))
                {
                    if (sb.Length > 0)
                        sb.Append(Separator);
                    sb.Append(id);
                }
            }

            return sb.ToString();
        }

        public override bool Execute()
        {
            if (Items == null)
                return true;

            var nsRoot = NamespaceRoot ?? "";

            foreach (var item in Items)
            {
                if (!string.IsNullOrEmpty(item.GetMetadata(NamespaceMetadata)))
                    continue;

                var dir = Path.GetDirectoryName(item.ItemSpec);

                var parts = dir.Split(PathSeparators);

                var ns = Combine(NamespaceRoot, Combine(parts));

                item.SetMetadata(NamespaceMetadata, ns);
            }

            Output = Items;
            return true;
        }
    }
}
