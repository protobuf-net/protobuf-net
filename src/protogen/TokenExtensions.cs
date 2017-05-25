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
        public static bool ConsumeIf(this Peekable<Token> tokens, TokenType type, string value)
        {
            if(tokens.Peek(out var token) && token.Is(type, value))
            {
                tokens.Consume();
                return true;
            }
            return false;
        }

        public static Token Read(this Peekable<Token> tokens)
        {
            if (!tokens.Peek(out Token val))
            {
                throw new ParserException(tokens.Previous, "Unexpected end of file", true);
            }
            return val;
        }
        public static bool SkipToEndOptions(this Peekable<Token> tokens)
        {
            while (tokens.Peek(out var token))
            {
                if (token.Is(TokenType.Symbol, ";") || token.Is(TokenType.Symbol, "}"))
                    return true; // but don't consume

                tokens.Consume();
                if (token.Is(TokenType.Symbol, "]"))
                    return true;
            }
            return false;
        }
        public static bool SkipToEndStatement(this Peekable<Token> tokens)
        {
            while (tokens.Peek(out var token))
            {
                if (token.Is(TokenType.Symbol, "}"))
                    return true; // but don't consume

                tokens.Consume();
                if (token.Is(TokenType.Symbol, ";"))
                    return true;
            }
            return false;
        }
        public static bool SkipToEndObject(this Peekable<Token> tokens) => SkipToSymbol(tokens, "}");
        private static bool SkipToSymbol(this Peekable<Token> tokens, string symbol)
        {
            while (tokens.Peek(out var token))
            {
                tokens.Consume();
                if (token.Is(TokenType.Symbol, symbol))
                    return true;
            }
            return false;
        }
        public static bool SkipToEndStatementOrObject(this Peekable<Token> tokens)
        {
            while (tokens.Peek(out var token))
            {
                tokens.Consume();
                if (token.Is(TokenType.Symbol, "}") || token.Is(TokenType.Symbol, ";"))
                    return true;
            }
            return false;
        }
        public static string Consume(this Peekable<Token> tokens, TokenType type)
        {
            var token = tokens.Read();
            token.Assert(type);
            string s = token.Value;
            tokens.Consume();
            return s;
        }

        internal static T ConsumeEnum<T>(this Peekable<Token> tokens, bool ignoreCase = true) where T : struct
        {
            var token = tokens.Read();
            var value = tokens.ConsumeString();

            if (!System.Enum.TryParse<T>(token.Value, ignoreCase, out T val))
                token.Throw("Unable to parse " + typeof(T).Name);
            return val;
        }

        internal static int ConsumeInt32(this Peekable<Token> tokens, int? max = null)
        {
            var token = tokens.Read();
            token.Assert(TokenType.AlphaNumeric);
            tokens.Consume();
            if (max.HasValue && token.Value == "max") return max.Value;

            if (!int.TryParse(token.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int val))
                token.Throw("Unable to parse integer");
            return val;
        }

        internal static string ConsumeString(this Peekable<Token> tokens)
        {
            var token = tokens.Read();
            switch (token.Type)
            {
                case TokenType.StringLiteral:
                case TokenType.AlphaNumeric:
                    tokens.Consume();
                    return token.Value;
                default:
                    throw token.Throw();
            }
        }

        internal static bool ConsumeBoolean(this Peekable<Token> tokens)
        {
            var token = tokens.Read();
            token.Assert(TokenType.AlphaNumeric);
            tokens.Consume();
            if (string.Equals("true", token.Value, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals("false", token.Value, StringComparison.OrdinalIgnoreCase)) return false;
            throw token.Throw("Unable to parse boolean");
        }

        internal static uint ConsumeUInt32(this Peekable<Token> tokens, uint? max = null)
        {
            var token = tokens.Read();
            token.Assert(TokenType.AlphaNumeric);
            tokens.Consume();
            if (max.HasValue && token.Value == "max") return max.Value;

            if (!uint.TryParse(token.Value, NumberStyles.None, CultureInfo.InvariantCulture, out uint val))
                token.Throw("Unable to parse integer");
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

        static bool CanCombine(TokenType type, char prev, char next)
            => type != TokenType.Symbol || prev == next;

        public static IEnumerable<Token> Tokenize(this TextReader reader)
        {
            var buffer = new StringBuilder();

            int lineNumber = 0, offset = 0;
            string line;
            string lastLine = null;
            while ((line = reader.ReadLine()) != null)
            {
                lastLine = line;
                lineNumber++;
                int columnNumber = 0, tokenStart = 1;
                char lastChar = '\0';
                TokenType type = TokenType.None;
                foreach (char c in line)
                {
                    columnNumber++;
                    if (type == TokenType.StringLiteral)
                    {
                        if (c == '"')
                        {
                            yield return new Token(buffer.ToString(), lineNumber, tokenStart, type, line, offset++);
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
                        if (newType == type && CanCombine(type, lastChar, c))
                        {
                            buffer.Append(c);
                        }
                        else
                        {
                            if (buffer.Length != 0)
                            {
                                yield return new Token(buffer.ToString(), lineNumber, tokenStart, type, line, offset++);
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
                    lastChar = c;
                }

                if (buffer.Length != 0)
                {
                    yield return new Token(buffer.ToString(), lineNumber, tokenStart, type, lastLine, offset++);
                    buffer.Clear();
                }
            }

        }
    }
}

