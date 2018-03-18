using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                foreach (var doc in proj.Documents)
                {
                    await Console.Out.WriteLineAsync($"Parsing {doc.Name}...");
                    var root = await doc.GetSyntaxRootAsync();

                    await Console.Out.WriteLineAsync($"Finding [ProtoContract]...");
                    var protoContractAttribs = root.DescendantNodes()
                     .OfType<AttributeSyntax>()
                     .Where(attrib => Is<ProtoContractAttribute>(attrib));


                    foreach (var pca in protoContractAttribs)
                    {
                        bool hasUnknown = false;
                        if (pca.ArgumentList != null)
                        {
                            foreach (var arg in pca.ArgumentList.Arguments)
                            {
                                await Console.Error.WriteLineAsync("unsupported: " + arg.ToString());
                                hasUnknown = true;
                            }
                        }
                        if (hasUnknown) continue;

                        try
                        {
                            if (!(pca.Parent?.Parent is TypeDeclarationSyntax typeDef)) continue;

                            if (Has<ProtoIgnoreAttribute>(typeDef, out var attrib)) continue;

                            if (Has<ProtoEnumAttribute>(typeDef, out attrib)
                                || Has<ProtoIncludeAttribute>(typeDef, out attrib)
                                || Has<ProtoConverterAttribute>(typeDef, out attrib)
                                || Has<ProtoPartialIgnoreAttribute>(typeDef, out attrib)
                                || Has<ProtoPartialMemberAttribute>(typeDef, out attrib)
                                )
                            {
                                await Console.Error.WriteLineAsync("not implemented: " + attrib.ToString());
                                continue;
                            }

                            await Console.Out.WriteLineAsync($"Found type {typeDef.Identifier.ToString()} with {typeDef.Members.Count} members; finding [ProtoMember]...");

                            foreach (var member in typeDef.Members)
                            {
                                try
                                {
                                    var pma = member.DescendantNodes().OfType<AttributeSyntax>()
                                        .SingleOrDefault(attr => Is<ProtoMemberAttribute>(attr));
                                    if (pma == null) continue;

                                    if (Has<ProtoIgnoreAttribute>(member, out attrib)) continue;

                                    if (Has<ProtoMapAttribute>(member, out attrib))
                                    {
                                        await Console.Error.WriteLineAsync("not implemented: " + attrib.ToString());
                                        continue;
                                    }

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
                                    hasUnknown = false;
                                    if (pma.ArgumentList != null)
                                    {
                                        foreach (var arg in pma.ArgumentList.Arguments)
                                        {
                                            var ne = arg.NameEquals;
                                            if (ne != null)
                                            {
                                                switch (ne.Name.Identifier.ToString())
                                                {
                                                    case nameof(ProtoMemberAttribute.DataFormat):
                                                        format = ParseFormat(arg.Expression);
                                                        break;
                                                    case nameof(ProtoMemberAttribute.Name):
                                                    case nameof(ProtoMemberAttribute.IsRequired):
                                                        break;
                                                    default:
                                                        await Console.Error.WriteLineAsync("unsupported: " + arg.ToString());
                                                        hasUnknown = true;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                var nc = arg.NameColon;
                                                if (nc != null)
                                                {
                                                    switch (nc.Name.Identifier.ToString())
                                                    {
                                                        case "tag":
                                                            tag = ParseTag(arg.Expression);
                                                            break;
                                                        default:
                                                            await Console.Error.WriteLineAsync("unsupported: " + arg.ToString());
                                                            hasUnknown = true;
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    tag = ParseTag(arg.Expression);
                                                }
                                            }
                                        }
                                    }
                                    if (hasUnknown || tag == null) continue;
                                    await Console.Out.WriteLineAsync($"  [{tag}/{format}] Found {name} of type {type}");

                                }
                                catch { } // anything odd: give up on that member
                            }
                        }
                        catch { } // anything odd: give up on that type
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
            switch (expression)
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
            switch (expression)
            {
                case LiteralExpressionSyntax literal: return (int)literal.Token.Value;
            }
            throw new NotSupportedException(expression.ToString());
        }
        static readonly ConcurrentDictionary<Type, HashSet<string>> attribOptions = new ConcurrentDictionary<Type, HashSet<string>>();
        static readonly char[] dot = { '.' };

        static bool Has<T>(TypeDeclarationSyntax type, out AttributeSyntax attrib) where T : Attribute
            => Has<T>(type.AttributeLists, out attrib);
        static bool Has<T>(SyntaxList<AttributeListSyntax> lists, out AttributeSyntax attrib) where T : Attribute
        {
            foreach (var list in lists)
            {
                foreach (var attr in list.Attributes)
                {
                    if (Is<T>(attr))
                    {
                        attrib = attr;
                        return true;
                    }
                }
            }
            attrib = null;
            return false;
        }

        static bool Has<T>(MemberDeclarationSyntax member, out AttributeSyntax attrib) where T : Attribute
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    return Has<T>(field, out attrib);
                case PropertyDeclarationSyntax prop:
                    return Has<T>(prop, out attrib);
            }
            attrib = null;
            return false;
        }

        static bool Has<T>(FieldDeclarationSyntax field, out AttributeSyntax attrib) where T : Attribute
            => Has<T>(field.AttributeLists, out attrib);
        static bool Has<T>(PropertyDeclarationSyntax prop, out AttributeSyntax attrib) where T : Attribute
            => Has<T>(prop.AttributeLists, out attrib);

        static bool Is<T>(AttributeSyntax attrib) where T : Attribute
        {
            HashSet<string> GetOptions(Type t)
            {
                var fn = t.FullName;
                var parts = fn.Split(dot, StringSplitOptions.RemoveEmptyEntries);
                var result = new HashSet<string>();
                if (parts.Length == 0) return result;

                bool endsAttribute = fn.EndsWith("Attribute");

                for (int i = 0; i < parts.Length; i++)
                {
                    var full = string.Join(".", parts.Skip(i));
                    result.Add(full);
                    if (endsAttribute) result.Add(full.Substring(0, full.Length - 9));
                }
                result.Remove(""); // just in case
                return result;
            }

            {
                var code = attrib.Name.ToString();
                int i = code.LastIndexOf("::");
                if (i >= 0) code = code.Substring(i + 2);

                return attribOptions.GetOrAdd(typeof(T),
                    t => GetOptions(t)).Contains(code);
            }
        }
        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }
    }
}
