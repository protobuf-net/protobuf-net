using System;

namespace ProtoBuf
{
    internal struct Token
    {
        public int LineNumber { get; }
        public int ColumnNumber { get; }
        public TokenType Type { get; }
        public string Value { get; }
        internal Token(string value, int lineNumber, int columnNumber, TokenType type)
        {
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            Type = type;
        }
        public override string ToString() => $"({LineNumber},{ColumnNumber}:{Type}) {Value}";

        internal Exception SyntaxError(string error = null) => throw new InvalidOperationException((error ?? "syntax error") + " at " + ToString());

        internal void Assert(TokenType type, string value = null)
        {
            if (value != null)
            {
                if (type != Type || value != Value) SyntaxError($"expected {type} '{value}'");

            }
            else
            {
                if (type != Type) SyntaxError($"expected {type}");
            }
        }

        internal bool Is(TokenType type, string value = null)
        {
            if (type != Type) return false;
            if (value != null && value != Value) return false;
            return true;
        }
    }
}
