using System;
using System.Collections.Generic;

namespace ProtoBuf
{
    class Enum : ProtoBase
    {
        public string Name { get; }
        internal Enum(string name)
        {
            Name = name;
        }
        internal static Enum TryParse(Peekable<Token> tokens, ProtoSyntax syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "enum");
            var obj = new Enum(tokens.Consume(TokenType.AlphaNumeric));
            tokens.Consume(TokenType.Symbol, "{");

            bool cont = true;
            while (cont)
            {
                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, "};"))
                {
                    tokens.Consume();
                    cont = false;
                }
                else if (token.Is(TokenType.Symbol, "}"))
                {
                    tokens.Consume();
                    // trailing semi-colon is optional and used inconsistently
                    if(tokens.Peek(out token) & token.Is(TokenType.Symbol, ";"))
                    {
                        tokens.Consume();
                    }
                    cont = false;
                }
                else
                {
                    obj.Values.Add(EnumValue.Parse(tokens, syntax));
                }
            }
            return obj;
        }
        public List<EnumValue> Values { get; } = new List<EnumValue>();
    }
}
