using Google.Protobuf.Reflection;
using System;

namespace ProtoBuf
{
    internal struct Token
    {

        public static bool operator ==(Token x, Token y)
        {
            return x.Offset == y.Offset;
        }
        public static bool operator !=(Token x, Token y)
        {
            return x.Offset != y.Offset;
        }
        public override int GetHashCode() => Offset;
        public override bool Equals(object obj) => (obj is Token) && ((Token)obj).Offset == this.Offset;
        public bool Equals(Token token) => token.Offset == this.Offset;
        public int Offset { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }
        public TokenType Type { get; }
        public string Value { get; }
        public string LineContents { get; }
        internal Token(string value, int lineNumber, int columnNumber, TokenType type, string lineContents, int offset)
        {
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            Type = type;
            LineContents = lineContents;
            Offset = offset;
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
            if(syntax != FileDescriptorProto.SyntaxProto2)
            {
                Throw("this feature requires " + FileDescriptorProto.SyntaxProto2 + " syntax");
            }
        }

        internal Error TypeNotFound(string typeName = null) => new Error(this,
            $"type not found: '{(string.IsNullOrWhiteSpace(typeName) ? Value : typeName)}'", true);
    }
}
