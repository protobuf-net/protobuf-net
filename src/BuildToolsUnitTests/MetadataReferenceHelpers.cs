using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.Meta;
using System.Linq;
using System.Reflection;

namespace BuildToolsUnitTests
{
    internal  static class MetadataReferenceHelpers
    {
        public static MetadataReference[] WellKnownReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location)
        };

        public static MetadataReference[] ProtoBufReferences = new[]
        {
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(TypeModel).Assembly.Location)
        };

        public static Project ReferenceMetadataReferences(this Project project, params MetadataReference[] metadataReferences)
        {
            if (metadataReferences is null || !metadataReferences.Any()) return project;
            foreach (var metadataReference in metadataReferences)
            {
                project = project.TryAddMetadataReference(metadataReference);
            }

            return project;
        }

        private static Project TryAddMetadataReference(this Project project, MetadataReference reference)
        {
            try
            {
                return project.AddMetadataReference(reference);
            }
            catch
            {
                // AddMetadataReference throws on attempt to add duplicate references,
                // so just ignore and continue in that case
            }

            return project;
        }
    }
}
