using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProtoBuf
{
    internal static class TokenExtensions
    {
        public static void Consume(this Peekable<Token> tokens, TokenType type, string value)
        {
            var token = tokens.Read();
            token.Assert(type, value);
            tokens.Consume();
        }
        public static string Consume(this Peekable<Token> tokens, TokenType type)
        {
            var token = tokens.Read();
            token.Assert(type);
            string s = token.Value;
            tokens.Consume();
            return s;
        }

        internal static T ConsumeEnum<T>(this Peekable<Token> tokens, TokenType type, bool ignoreCase = true) where T : struct
        {
            var token = tokens.Read();
            token.Assert(type);
            tokens.Consume();

            if (!System.Enum.TryParse<T>(token.Value, ignoreCase, out T val))
                token.SyntaxError("Unable to parse " + typeof(T).Name);
            return val;
        }
        internal static uint ConsumeUInt32(this Peekable<Token> tokens, uint? max = null)
        {
            var token = tokens.Read();
            token.Assert(TokenType.AlphaNumeric);
            tokens.Consume();
            if (max.HasValue && token.Value == "max") return max.Value;

            if (!uint.TryParse(token.Value, NumberStyles.None, CultureInfo.InvariantCulture, out uint val))
                token.SyntaxError("Unable to parse integer");
            return val;
        }

        static TokenType Identify(char c)
        {
            if (c == '"') return TokenType.StringLiteral;
            if (char.IsWhiteSpace(c)) return TokenType.Whitespace;
            if (char.IsLetterOrDigit(c)) return TokenType.AlphaNumeric;
            switch(c)
            {
                case '_':
                case '.':
                    return TokenType.AlphaNumeric;
            }
            return TokenType.Symbol;
        }

        public static IEnumerable<Token> RemoveCommentsAndWhitespace(this IEnumerable<Token> tokens)
        {
            int commentLineNumber = -1;
            foreach (var token in tokens)
            {
                if (commentLineNumber == token.LineNumber)
                {
                    // swallow everything else on that line
                }
                else if (token.Type == TokenType.Whitespace)
                {
                    continue;
                }
                else if (token.Type == TokenType.Symbol && token.Value.StartsWith("//"))
                {
                    commentLineNumber = token.LineNumber;
                }
                else
                {
                    yield return token;
                }
            }
        }
        public static IEnumerable<Token> Tokenize(this TextReader reader)
        {
            var buffer = new StringBuilder();

            int lineNumber = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                int columnNumber = 0, tokenStart = 1;
                TokenType type = TokenType.None;
                foreach (char c in line)
                {
                    columnNumber++;
                    if (type == TokenType.StringLiteral)
                    {
                        if (c == '"')
                        {
                            yield return new Token(buffer.ToString(), lineNumber, tokenStart, type);
                            buffer.Clear();
                            type = TokenType.None;
                        }
                        else
                        {
                            buffer.Append(c);
                        }
                    }
                    else
                    {
                        var newType = Identify(c);
                        if (newType == type)
                        {
                            buffer.Append(c);
                        }
                        else
                        {
                            if (buffer.Length != 0)
                            {
                                yield return new Token(buffer.ToString(), lineNumber, tokenStart, type);
                                buffer.Clear();
                            }
                            type = newType;
                            tokenStart = columnNumber;
                            if (newType != TokenType.StringLiteral)
                            {
                                buffer.Append(c);
                            }
                        }
                    }
                }

                if (buffer.Length != 0)
                {
                    yield return new Token(buffer.ToString(), lineNumber, tokenStart, type);
                    buffer.Clear();
                }
            }

        }
    }
}

