using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                await Console.Out.WriteLineAsync("Initializing Rosyn/MSBuid...");
                var ws = MSBuildWorkspace.Create();
                await Console.Out.WriteLineAsync("Loading project...");
                var proj = await ws.OpenProjectAsync(@"..\..\..\..\CSharpTest\CSharpTest.csproj");
                await Console.Out.WriteLineAsync($"Loaded {proj.Name}");
                foreach(var doc in proj.Documents)
                {
                    await Console.Out.WriteLineAsync($"Parsing {doc.Name}...");
                    var root = await doc.GetSyntaxRootAsync();

                    await Console.Out.WriteLineAsync($"Finding [ProtoContract]...");
                    var protoContractAttribs = root.DescendantNodes()
                     .OfType<AttributeSyntax>()
                     .Where(attrib => IsProtoContractAttribute(attrib));

                    foreach (var pca in protoContractAttribs)
                    {
                        try {
                            if (!(pca.Parent?.Parent is TypeDeclarationSyntax typeDef)) continue;
                            await Console.Out.WriteLineAsync($"Found type {typeDef.Identifier.ToString()} with {typeDef.Members.Count} members; finding [ProtoMember]...");

                            foreach (var member in typeDef.Members)
                            {
                                try
                                {
                                    var pma = member.DescendantNodes().OfType<AttributeSyntax>()
                                        .SingleOrDefault(attrib => IsProtoMemberAttribute(attrib));
                                    if (pma == null) continue;

                                    TypeSyntax type;
                                    SyntaxToken name;
                                    switch (member)
                                    {
                                        case FieldDeclarationSyntax fields:
                                            var field = fields.Declaration.Variables.Single();
                                            type = fields.Declaration.Type;
                                            name = field.Identifier;
                                            break;
                                        case PropertyDeclarationSyntax property:
                                            type = property.Type;
                                            name = property.Identifier;
                                            break;
                                        default:
                                            continue; // your guesss is as good as mine
                                    }
                                    int? tag = null;
                                    DataFormat? format = null;
                                    foreach(var arg in pma.ArgumentList.Arguments)
                                    {
                                        var ne = arg.NameEquals;
                                        if(ne != null)
                                        {
                                            switch (ne.Name.Identifier.ToString())
                                            {
                                                case "DataFormat":
                                                    format = ParseFormat(arg.Expression);
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            var nc = arg.NameColon;
                                            if(nc != null)
                                            {
                                                switch(nc.Name.Identifier.ToString())
                                                {
                                                    case "tag": tag = ParseTag(arg.Expression); break;
                                                }
                                            }
                                            else
                                            {
                                                tag = ParseTag(arg.Expression);
                                            }
                                        }
                                    }
                                    if (tag == null) continue;
                                    await Console.Out.WriteLineAsync($"  [{tag}/{format}] Found {name} of type {type}");

                                }
                                catch { } // anything odd: give up on that member
                            }
                        } catch { } // anything odd: give up on that type
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var inner in ex.LoaderExceptions)
                {
                    await Console.Error.WriteLineAsync(inner.Message);
                }
            }
        }

        private static DataFormat ParseFormat(ExpressionSyntax expression)
        {
            switch(expression)
            {
                case MemberAccessExpressionSyntax mae:
                    var s = mae.ToFullString();
                    var dot = s.LastIndexOf('.');
                    if (dot < 0) break; // no clue
                    s = s.Substring(dot + 1);
                    return (DataFormat)Enum.Parse(typeof(DataFormat), s, false);
            }
            throw new NotSupportedException(expression.ToString());
        }

        private static int ParseTag(ExpressionSyntax expression)
        {
            switch(expression)
            {
                case LiteralExpressionSyntax literal: return (int)literal.Token.Value;    
            }
            throw new NotSupportedException(expression.ToString());
        }

        static bool IsProtoContractAttribute(AttributeSyntax attrib)
        {
            switch(attrib.Name.ToString())
            {
                case "ProtoContract":
                case "ProtoContractAttribute":
                case "ProtoBuf.ProtoContract":
                case "ProtoBuf.ProtoContractAttribute":
                case "global::ProtoBuf.ProtoContract":
                case "global::ProtoBuf.ProtoContractAttribute":
                    return true;
                default:
                    return false;
            }
        }
        static bool IsProtoMemberAttribute(AttributeSyntax attrib)
        {
            switch (attrib.Name.ToString())
            {
                case "ProtoMember":
                case "ProtoMemberAttribute":
                case "ProtoBuf.ProtoMember":
                case "ProtoBuf.ProtoMemberAttribute":
                case "global::ProtoBuf.ProtoMember":
                case "global::ProtoBuf.ProtoMemberAttribute":
                    return true;
                default:
                    return false;
            }
        }
        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }
    }
}
