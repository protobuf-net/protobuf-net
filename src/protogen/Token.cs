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

        internal Error Error(string error = null, bool isError = true) =>
            new Error(this, string.IsNullOrWhiteSpace(error) ? $"syntax error: '{Value}'" : error, isError);
        internal Exception Throw(string error = null, bool isError = true) =>
            throw new ParserException(this, string.IsNullOrWhiteSpace(error) ? $"syntax error: '{Value}'" : error, isError);

        internal void Assert(TokenType type, string value = null)
        {
            if (value != null)
            {
                if (type != Type || value != Value) Throw($"expected {type} '{value}'");

            }
            else
            {
                if (type != Type) Throw($"expected {type}");
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
                Throw("This feature requires " + Parsers.SyntaxProto2 + " syntax");
            }
        }

        internal Error TypeNotFound(string typeName = null) => new Error(this,
            $"type not found: '{(string.IsNullOrWhiteSpace(typeName) ? Value : typeName)}'", true);
    }
}
