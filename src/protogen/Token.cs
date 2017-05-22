using System;

namespace ProtoBuf
{
    internal struct Token
    {
        public int LineNumber { get; }
        public int ColumnNumber { get; }
        public TokenType Type { get; }
        public string Value { get; }
        public string LineContents { get; }
        internal Token(string value, int lineNumber, int columnNumber, TokenType type, string lineContents)
        {
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            Type = type;
            LineContents = lineContents;
        }
        public override string ToString() => $"({LineNumber},{ColumnNumber}) '{Value}'";

        internal Exception SyntaxError(string error = null) => throw new InvalidOperationException((error ?? "syntax error") + " at " + ToString() + ": " + LineContents);

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

        internal void RequireProto2(string syntax)
        {
            if(syntax != Parsers.SyntaxProto2)
            {
                SyntaxError("This feature requires " + Parsers.SyntaxProto2 + " syntax");
            }
        }
    }
}
