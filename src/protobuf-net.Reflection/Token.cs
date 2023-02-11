using Google.Protobuf.Reflection;
using ProtoBuf.Reflection.Internal;
using System;

namespace ProtoBuf.Reflection
{
    internal readonly struct Token : IEquatable<Token>
    {
        public static bool operator ==(in Token x, in Token y)
        {
            return x.TokenIndex == y.TokenIndex && x.File == y.File;
        }
        public static bool operator !=(in Token x, in Token y)
        {
            return x.TokenIndex != y.TokenIndex || x.File != y.File;
        }
        public override int GetHashCode() => TokenIndex;
        public override bool Equals(object obj) => obj is Token other && other.TokenIndex == this.TokenIndex && other.File == this.File;
        public bool Equals(in Token other) => other.TokenIndex == this.TokenIndex && other.File == this.File;
        bool IEquatable<Token>.Equals(Token other) => Equals(in other);

        public int TokenIndex { get; }
        public int LineNumber { get; }
        public string File { get; }
        public int ColumnNumber { get; }
        public TokenType Type { get; }
        public string Value { get; }
        public string LineContents { get; }
        internal Token(string value, int lineNumber, int columnNumber, TokenType type, string lineContents, int tokenIndex, string file)
        {
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            File = file;
            Type = type;
            LineContents = lineContents;
            TokenIndex = tokenIndex;
        }
        public override string ToString() => $"({LineNumber},{ColumnNumber}) '{Value}'";

        internal Exception Throw(ErrorCode errorCode, string error = null, bool isError = true) =>
            throw new ParserException(this, string.IsNullOrWhiteSpace(error) ? $"syntax error: '{Value}'" : error, isError, errorCode);

        internal void Assert(TokenType type, string value = null)
        {
            if (value != null)
            {
                if (type != Type || value != Value)
                {
                    Throw(ErrorCode.ExpectedToken, $"expected {type} '{value}'");
                }
            }
            else
            {
                if (type != Type)
                {
                    Throw(ErrorCode.ExpectedToken, $"expected {type}");
                }
            }
        }

        internal bool Is(TokenType type, string value = null)
        {
            if (type != Type) return false;
            if (value != null && value != Value) return false;
            return true;
        }

        internal void RequireProto2(ParserContext ctx)
        {
            if(ctx.Syntax != FileDescriptorProto.SyntaxProto2)
            {
                var msg = "'" + Value + "' requires " + FileDescriptorProto.SyntaxProto2 + " syntax";
                ctx.Errors.Error(this, msg, ErrorCode.ProtoSyntaxRequireProto2);
            }
        }

        internal Error TypeNotFound(string typeName = null) => new Error(this,
            $"type not found: '{(string.IsNullOrWhiteSpace(typeName) ? Value : typeName)}'", true, ErrorCode.TypeNotFound);
    }
}
