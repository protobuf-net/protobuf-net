using System;

namespace ProtoBuf
{
    public class Field : ProtoBase
    {
        public Options Options { get; } = new Options();
        public override string ToString() => $"{Number}: {Name} ({Type})";
        public string Type { get; }
        public string Name { get; }
        public uint Number { get; }

        private Field(string type, string name, uint number, FieldModifiers modifiers)
        {
            Type = type;
            Name = name;
            Number = number;
            Modifiers = modifiers;
        }
        [Flags]
        public enum FieldModifiers
        {
            None = 0,
            Repeated = 1,
            Required = 2,
            Optional = 4
        }

        public FieldModifiers Modifiers { get; }
        internal static Field Parse(Peekable<Token> tokens, ProtoSyntax syntax)
        {
            FieldModifiers modifiers = FieldModifiers.None;

            var token = tokens.Read();
            if (token.Is(TokenType.AlphaNumeric, "repeated"))
            {
                modifiers = FieldModifiers.Repeated;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "required"))
            {
                modifiers = FieldModifiers.Required;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "optional"))
            {
                modifiers = FieldModifiers.Optional;
                tokens.Consume();
            }

            string type = tokens.Consume(TokenType.AlphaNumeric);
            string fieldName = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            uint number = tokens.ConsumeUInt32();

            var field = new Field(type, fieldName, number, modifiers);
            bool haveEndedSemicolon = false;
            if (tokens.Read().Is(TokenType.Symbol, "["))
            {
                haveEndedSemicolon = field.Options.ParseForField(tokens);
            }

            if (!haveEndedSemicolon)
            {
                tokens.Consume(TokenType.Symbol, ";");
            }
            return field;
        }
    }
}
