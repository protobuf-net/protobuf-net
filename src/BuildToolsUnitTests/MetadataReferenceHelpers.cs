using Microsoft.CodeAnalysis;
using ProtoBuf.Meta;
using System.Reflection;

namespace BuildToolsUnitTests
{
    internal  static class MetadataReferenceHelpers
    {
        public static MetadataReference[] ProtoBufReferences = new[]
        {
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(TypeModel).Assembly.Location)
        };

        public static Project ReferenceProtoBuf(this Project project)
        {
            foreach (var reference in ProtoBufReferences)
            {
                project = project.AddMetadataReference(reference);
            }

            return project;
        }
    }
}
