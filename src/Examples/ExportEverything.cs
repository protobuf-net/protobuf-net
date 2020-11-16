using Examples;
using Examples.Issues;
using ProtoBuf.Issues;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class ExportEverything
    {
        // [Fact]
        public void ExportEverythingInExamples()
        {
            var path = Path.ChangeExtension(nameof(ExportEverything), "dll");
            var except = new HashSet<Type>
            {
                typeof(Issue41.A_Orig),
                typeof(Issue41.B_Orig),
                typeof(WithIP),
                typeof(Examples.Issues.I),
                typeof(Examples.Issues.O),
                typeof(Examples.Issues.OS),
                typeof(Examples.Issues.C),
                typeof(Examples.Issues.CS),
                typeof(Issue509.Item),
                typeof(Issue509.ItemSurrogate),
                typeof(ImmutableCollections.ImmutableConcreteFields),
                typeof(ImmutableCollections.ImmutableConcreteProperties),
                typeof(ImmutableCollections.ImmutableInterfaceFields),
                typeof(ImmutableCollections.ImmutableInterfaceProperties),
                typeof(ProtoGeneration.MyNonSurrogate),
                typeof(ProtoGeneration.MySurrogate),
                typeof(ProtoGeneration.UsesSurrogates),
                typeof(StupidlyComplexModel.SimpleModel),
                typeof(AssortedGoLiveRegressions.HasBytes),
#if NETFRAMEWORK
                typeof(Issue124.TypeWithColor),
#endif
                typeof(Issue184.A),
                typeof(Issue184.B),
                typeof(Issue218.Test),
                typeof(Issue222.Foo),
                typeof(Issue222.Bar),
                typeof(Issue374.Issue374TestModel),

            };
            var options = new RuntimeTypeModel.CompilerOptions
            {
                TypeName = nameof(ExportEverything),
#pragma warning disable CS0618
                OutputPath = path,
#pragma warning restore CS0618
            };
            options.IncludeType += t =>
            {
                return !except.Contains(t);
            };

            AutoCompileTypeModel.CreateForAssembly(GetType().Assembly, options);
            PEVerify.AssertValid(path);
        }
    }
}
