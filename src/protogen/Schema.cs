using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtoBuf
{
    public sealed class Schema
    {
        public Options Options { get; } = new Options();
        private Schema() { }
        public static Schema Parse(TextReader schema)
        {
            var parsed = new Schema();
            using (var tokens = new Peekable<Token>(schema.Tokenize().RemoveCommentsAndWhitespace()))
            {
                while (tokens.Peek(out Token token))
                {
                    if (ProtoBase.TryParse(tokens, parsed.Syntax, out var item))
                    {
                        parsed.Items.Add(item);
                    }
                    else if (token.Is(TokenType.AlphaNumeric))
                    {
                        switch (token.Value)
                        {
                            case "syntax":
                                if (parsed.Items.Any())
                                {
                                    token.SyntaxError("must preceed all other instructions");
                                }
                                tokens.Consume();
                                tokens.Consume(TokenType.Symbol, "=");
                                parsed.Syntax = tokens.ConsumeEnum<ProtoSyntax>(TokenType.StringLiteral);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "package":
                                tokens.Consume();
                                parsed.Package = tokens.Consume(TokenType.AlphaNumeric);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "option":
                                parsed.Options.ParseForSchema(tokens);
                                break;
                            default:
                                token.SyntaxError();
                                break;
                        }
                    }
                    else
                    {
                        token.SyntaxError();
                    }
                }
            }
            return parsed;
        }
        public IEnumerable<Message> Messages => Items.OfType<Message>();
        public List<ProtoBase> Items { get; } = new List<ProtoBase>();
        public ProtoSyntax Syntax { get; private set; } = ProtoSyntax.proto2;
        public string Package { get; private set; } = "";
    }


}
