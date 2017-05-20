using System;

namespace ProtoBuf
{
    public class EnumValue : ProtoBase
    {
        public string Name { get; }
        public string Value { get; }

        internal EnumValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        internal static EnumValue Parse(Peekable<Token> tokens, ProtoSyntax syntax)
        {
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            string value = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, ";");
            return new EnumValue(name, value);
        }
    }
}