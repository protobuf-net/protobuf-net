using System;
using System.Collections.Generic;

namespace ProtoBuf
{
    public class Options
    {
        private readonly Dictionary<string, string> lookup
            = new Dictionary<string, string>();
        public string this[string key]
        {
            get
            {
                return lookup.TryGetValue(key, out var value) ? value : null;
            }
            private set
            {
                if (value == null) lookup.Remove(key);
                else lookup[key] = value;
            }
        }

        internal void ParseForSchema(Peekable<Token> tokens)
        {
            tokens.Consume(TokenType.AlphaNumeric, "option");
            Parse(tokens);
            tokens.Consume(TokenType.Symbol, ";");
        }
        internal void Parse(Peekable<Token> tokens)
        {
            var key = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var val = tokens.Read();
            switch (val.Type)
            {
                case TokenType.AlphaNumeric:
                case TokenType.StringLiteral:
                    this[key] = val.Value;
                    tokens.Consume();
                    break;
                default:
                    throw val.SyntaxError("Expected option value");
            }
        }
        internal bool ParseForField(Peekable<Token> tokens)
        {
            tokens.Consume(TokenType.Symbol, "[");
            while(true)
            {
                var token = tokens.Read();
                if(token.Is(TokenType.Symbol, "]"))
                {
                    tokens.Consume();
                    return false;
                }
                else if (token.Is(TokenType.Symbol, "];"))
                {
                    tokens.Consume();
                    return true;
                }
                else if (token.Is(TokenType.Symbol, ","))
                {
                    tokens.Consume();
                }
                else
                {
                    Parse(tokens);
                }
            }
        }
    }
}
