using ProtoBuf.Reflection.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProtoBuf.Reflection
{
    internal static class TokenExtensions
    {
        public static bool Is(this Peekable<Token> tokens, TokenType type, string value = null)
            => tokens.Peek(out var val) && val.Is(type, value);

        public static void Consume(this Peekable<Token> tokens, TokenType type, string value)
        {
            var token = tokens.Read();
            token.Assert(type, value);
            tokens.Consume();
        }
        public static bool ConsumeIf(this Peekable<Token> tokens, TokenType type, string value)
        {
            if (tokens.Peek(out var token) && token.Is(type, value))
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
                throw new ParserException(tokens.Previous, "Unexpected end of file", true, ErrorCode.UnexpectedEOF);
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
            => Consume(tokens, type, out _);
        public static string Consume(this Peekable<Token> tokens, TokenType type, out Token token)
        {
            token = tokens.Read();
            token.Assert(type);
            string s = token.Value;
            tokens.Consume();
            return s;
        }

        private static class EnumCache<T>
        {
            private static readonly Dictionary<string, T> lookup;
            public static bool TryGet(string name, out T value) => lookup.TryGetValue(name, out value);
            static EnumCache()
            {
                var fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public);
                var tmp = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in fields)
                {
                    string name = field.Name;
                    var attrib = (ProtoEnumAttribute)field.GetCustomAttributes(false).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(attrib?.Name)) name = attrib.Name;
                    var val = (T)field.GetValue(null);
                    tmp.Add(name, val);
                }
                lookup = tmp;
            }
        }
        internal static T ConsumeEnum<T>(this Peekable<Token> tokens) where T : struct
        {
            var token = tokens.Read();
            var value = tokens.ConsumeString();

            if (!EnumCache<T>.TryGet(value, out T val))
                token.Throw(ErrorCode.InvalidEnum, "Unable to parse " + typeof(T).Name);
            return val;
        }

        internal static bool TryParseUInt32(string token, out uint val, uint? max = null)
        {
            if (max.HasValue && token == "max")
            {
                val = max.GetValueOrDefault();
                return true;
            }

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && uint.TryParse(token.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                return true;
            }

            return uint.TryParse(token, NumberStyles.Integer | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out val);
        }
        internal static bool TryParseUInt64(string token, out ulong val, ulong? max = null)
        {
            if (max.HasValue && token == "max")
            {
                val = max.GetValueOrDefault();
                return true;
            }

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && ulong.TryParse(token.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                return true;
            }

            return ulong.TryParse(token, NumberStyles.Integer | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out val);
        }
        internal static bool TryParseInt32(string token, out int val, int? max = null)
        {
            if (max.HasValue && token == "max")
            {
                val = max.GetValueOrDefault();
                return true;
            }

            if (token.StartsWith("-0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(token.Substring(3), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                val = -val;
                return true;
            }

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(token.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                return true;
            }

            return int.TryParse(token, NumberStyles.Integer | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out val);
        }
        internal static bool TryParseInt64(string token, out long val, long? max = null)
        {
            if (max.HasValue && token == "max")
            {
                val = max.GetValueOrDefault();
                return true;
            }

            if (token.StartsWith("-0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(token.Substring(3), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                val = -val;
                return true;
            }

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(token.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out val))
            {
                return true;
            }

            return long.TryParse(token, NumberStyles.Integer | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out val);
        }
        internal static int ConsumeInt32(this Peekable<Token> tokens, int? max = null)
        {
            var token = tokens.Read();
            token.Assert(TokenType.AlphaNumeric);
            tokens.Consume();

            if (TryParseInt32(token.Value, out int val, max)) return val;
            throw token.Throw(ErrorCode.InvalidInteger, "Unable to parse integer");
        }

        internal static string ConsumeString(this Peekable<Token> tokens, bool asBytes = false)
        {
            var token = tokens.Read();
            switch (token.Type)
            {
                case TokenType.StringLiteral:
                    MemoryStream ms = null;
                    do
                    {
                        ReadStringBytes(ref ms, in token, tokens.Errors, !asBytes);
                        tokens.Consume();
                    } while (tokens.Peek(out token) && token.Type == TokenType.StringLiteral); // literal concat is a thing
                    if (ms == null) return "";

                    if (!asBytes)
                    {
                        var buffer = ms.GetBuffer();
                        return Encoding.UTF8.GetString(buffer, 0, (int)ms.Length);
                    }

                    var sb = new StringBuilder((int)ms.Length);
                    int b;
                    ms.Position = 0;
                    while ((b = ms.ReadByte()) >= 0)
                    {
                        switch (b)
                        {
                            case '\n': sb.Append(@"\n"); break;
                            case '\r': sb.Append(@"\r"); break;
                            case '\t': sb.Append(@"\t"); break;
                            case '\'': sb.Append(@"\'"); break;
                            case '\"': sb.Append(@"\"""); break;
                            case '\\': sb.Append(@"\\"); break;
                            default:
                                if (b >= 32 && b < 127)
                                {
                                    sb.Append((char)b);
                                }
                                else
                                {
                                    // encode as 3-part octal
                                    sb.Append('\\')
                                          .Append((char)(((b >> 6) & 7) + (int)'0'))
                                          .Append((char)(((b >> 3) & 7) + (int)'0'))
                                          .Append((char)(((b >> 0) & 7) + (int)'0'));
                                }
                                break;
                        }
                    }
                    return sb.ToString();
                case TokenType.AlphaNumeric:
                    tokens.Consume();
                    return token.Value;
                default:
                    throw token.Throw(ErrorCode.InvalidString);
            }
        }

        // the normalized output *includes* the slashes, but expands octal to 3 places;
        // it is the job of codegen to change this normalized form to the target language form
        internal static void ReadStringBytes(ref MemoryStream ms, in Token token, List<Error> errors, bool validateUnicode)
        {
            static void AppendAscii(MemoryStream target, string ascii)
            {
                foreach (char c in ascii)
                    target.WriteByte(checked((byte)c));
            }
            static void AppendByte(MemoryStream target, ref uint codePoint, ref int len, in Token token, List<Error> errors, int index, ref bool haveRawUnicode)
            {
                if (len != 0)
                {
                    target.WriteByte(checked((byte)codePoint));
                    if (codePoint > 127)
                    {
                        haveRawUnicode = true;
                    }
                }
                codePoint = 0;
                len = 0;
            }
            unsafe static void AppendNormalized(MemoryStream target, ref uint codePoint, ref int len)
            {
                if (len != 0)
                {
                    if (codePoint < 0x10000) // BMP
                    {
                        byte* b = stackalloc byte[10];
                        char c = checked((char)codePoint);
                        int count = Encoding.UTF8.GetBytes(&c, 1, b, 10);
                        for (int i = 0; i < count; i++)
                        {
                            target.WriteByte(b[i]);
                        }
                    }
                    else
                    {
                        var s = char.ConvertFromUtf32(checked((int)codePoint));
                        var bLen = Encoding.UTF8.GetMaxByteCount(s.Length);
                        byte* b = stackalloc byte[bLen];
                        fixed (char* sPtr = s)
                        {
                            int count = Encoding.UTF8.GetBytes(sPtr, s.Length, b, bLen);
                            for (int i = 0; i < count; i++)
                            {
                                target.WriteByte(b[i]);
                            }
                        }
                    }
                }
                codePoint = 0;
                len = 0;
            }
            static void AppendEscaped(MemoryStream target, char c)
            {
                uint codePoint = c switch
                {
                    'a' => '\a',
                    'b' => '\b',
                    'f' => '\f',
                    'v' => '\v',
                    't' => '\t',
                    'n' => '\n',
                    'r' => '\r',
                    '\\' or '?' or '\'' or '\"' => c,
                    _ => '?',
                };
                int len = 1;
                AppendNormalized(target, ref codePoint, ref len);
            }

            const char STATE_NORMAL = '_', STATE_ESCAPE = '\\', STATE_OCTAL = '0', STATE_HEX = 'x', STATE_UTF16 = 'u', STATE_UTF32 = 'U';
            char state = STATE_NORMAL;

            bool haveRawUnicode = false;
            var value = token.Value;
            if (string.IsNullOrEmpty(value)) return;

            if (ms == null) ms = new MemoryStream(value.Length);
            uint escapedCodePoint = 0;
            int escapeLength = 0;
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (state)
                {
                    case STATE_ESCAPE:
                        switch (c)
                        {
                            case 'x':
                                state = STATE_HEX;
                                break;
                            case 'u':
                                state = STATE_UTF16;
                                break;
                            case 'U':
                                state = STATE_UTF32;
                                break;
                            default:
                                if (c >= '0' && c <= '7')
                                {
                                    state = STATE_OCTAL;
                                    GetHexValue(c, out escapedCodePoint, ref escapeLength); // not a typo; all 1-char octal values are also the same in hex
                                }
                                else
                                {
                                    state = STATE_NORMAL;
                                    AppendEscaped(ms, c);
                                }
                                break;
                        }
                        break;
                    case STATE_OCTAL:
                        if (c >= '0' && c <= '7')
                        {
                            GetHexValue(c, out var x, ref escapeLength);
                            escapedCodePoint = (escapedCodePoint << 3) | x;
                            if (escapeLength == 3)
                            {
                                AppendByte(ms, ref escapedCodePoint, ref escapeLength, token, errors, i, ref haveRawUnicode);
                                state = STATE_NORMAL;
                            }
                        }
                        else
                        {
                            // not an octal char - regular append
                            if (escapeLength == 0)
                            {
                                // include the malformed \x
                                AppendAscii(ms, @"\x");
                            }
                            else
                            {
                                AppendByte(ms, ref escapedCodePoint, ref escapeLength, token, errors, i, ref haveRawUnicode);
                            }
                            state = STATE_NORMAL;
                            goto case STATE_NORMAL;
                        }
                        break;
                    case STATE_UTF16:
                        {
                            if (GetHexValue(c, out var x, ref escapeLength))
                            {
                                escapedCodePoint = (escapedCodePoint << 4) | x;
                                if (escapeLength == 4)
                                {
                                    AppendNormalized(ms, ref escapedCodePoint, ref escapeLength);
                                    state = STATE_NORMAL;
                                }
                            }
                            else
                            {
                                // not a hex char - regular append (note: this is invalid)
                                errors.Error(token, "Invalid \\u escape sequence in string literal.", ErrorCode.InvalidEscapeSequence);
                                AppendNormalized(ms, ref escapedCodePoint, ref escapeLength);
                                state = STATE_NORMAL;
                                goto case STATE_NORMAL;
                            }
                        }
                        break;
                    case STATE_UTF32:
                        {
                            if (GetHexValue(c, out var x, ref escapeLength))
                            {
                                escapedCodePoint = (escapedCodePoint << 4) | x;
                                if (escapeLength == 8)
                                {
                                    AppendNormalized(ms, ref escapedCodePoint, ref escapeLength);
                                    state = STATE_NORMAL;
                                }
                            }
                            else
                            {
                                // not a hex char - regular append (note: this is invalid)
                                errors.Error(token, "Invalid \\U escape sequence in string literal.", ErrorCode.InvalidEscapeSequence);
                                AppendNormalized(ms, ref escapedCodePoint, ref escapeLength);
                                state = STATE_NORMAL;
                                goto case STATE_NORMAL;
                            }
                        }
                        break;
                    case STATE_HEX:
                        {
                            if (GetHexValue(c, out var x, ref escapeLength))
                            {
                                escapedCodePoint = (escapedCodePoint << 4) | x;
                                if (escapeLength == 2)
                                {
                                    AppendByte(ms, ref escapedCodePoint, ref escapeLength, token, errors, i, ref haveRawUnicode);
                                    state = STATE_NORMAL;
                                }
                            }
                            else
                            {
                                // not a hex char - regular append
                                AppendByte(ms, ref escapedCodePoint, ref escapeLength, token, errors, i, ref haveRawUnicode);
                                state = STATE_NORMAL;
                                goto case STATE_NORMAL;
                            }
                        }
                        break;
                    case STATE_NORMAL:
                        if (c == '\\')
                        {
                            state = STATE_ESCAPE;
                        }
                        else
                        {
                            uint codePoint = (uint)c;
                            int len = 1;
                            AppendNormalized(ms, ref codePoint, ref len);
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            switch (state)
            {
                case STATE_NORMAL:
                    break; // fine
                case STATE_HEX:
                case STATE_OCTAL:
                    // these are allowed to terminate prematurely; append any trailing escaped data
                    AppendByte(ms, ref escapedCodePoint, ref escapeLength, token, errors, value.Length, ref haveRawUnicode);
                    break;
                case STATE_ESCAPE:
                    errors.Error(token, "Escape characters must be terminated in the same literal.", ErrorCode.InvalidEscapeSequence);
                    break;
            }

            if (validateUnicode && haveRawUnicode)
            {
                ValidateUtf8(token, ms, errors);
            }
        }

        internal static bool ValidateUtf8(in Token token, MemoryStream ms, List<Error> errors)
        {
            // protoc does not guard against invalid input, so for compatibility we can't just "fix" things;
            // we can at least tell the user about the problem, though
            var buffer = ms.GetBuffer();
            var oversizedChars = ArrayPool<char>.Shared.Rent(Encoding.UTF8.GetMaxCharCount((int)ms.Length));
            var decodedChars = Encoding.UTF8.GetChars(buffer, 0, (int)ms.Length, oversizedChars, 0);
            var oversizedBytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(decodedChars));
            var encodedBytes = Encoding.UTF8.GetBytes(oversizedChars, 0, decodedChars, oversizedBytes, 0);

            var same = ms.Length == encodedBytes;
            for (int i = 0; i < encodedBytes && same; i++)
            {
                same = oversizedBytes[i] == buffer[i];
            }
            if (!same)
            {
                errors.Warn(token, "Invalid UTF8 detected; it may be preferable to use \\uNNNN or \\U00NNNNNN instead of \\xN[N]", ErrorCode.InvalidUtf8);
            }
            ArrayPool<byte>.Shared.Return(oversizedBytes);
            ArrayPool<char>.Shared.Return(oversizedChars);
            return same;
        }

        internal static bool GetHexValue(char c, out uint val, ref int len)
        {
            len++;
            if (c >= '0' && c <= '9')
            {
                val = (uint)c - (uint)'0';
                return true;
            }
            if (c >= 'a' && c <= 'f')
            {
                val = 10 + (uint)c - (uint)'a';
                return true;
            }
            if (c >= 'A' && c <= 'F')
            {
                val = 10 + (uint)c - (uint)'A';
                return true;
            }
            len--;
            val = 0;
            return false;
        }

        internal static bool ConsumeBoolean(this Peekable<Token> tokens)
        {
            var token = tokens.Read();
            token.Assert(TokenType.AlphaNumeric);
            tokens.Consume();
            if (string.Equals("true", token.Value, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals("false", token.Value, StringComparison.OrdinalIgnoreCase)) return false;
            throw token.Throw(ErrorCode.InvalidBoolean, "Unable to parse boolean");
        }

        private static TokenType Identify(char c)
        {
            if (c == '"' || c == '\'') return TokenType.StringLiteral;
            if (char.IsWhiteSpace(c)) return TokenType.Whitespace;
            if (char.IsLetterOrDigit(c)) return TokenType.AlphaNumeric;
            return c switch
            {
                '_' or '.' or '-' => TokenType.AlphaNumeric,
                _ => TokenType.Symbol,
            };
        }

        public static IEnumerable<Token> RemoveCommentsAndWhitespace(this IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                if (token.Is(TokenType.Comment) || token.Is(TokenType.Whitespace))
                {
                }
                else
                {
                    yield return token;
                }
            }
        }

        public static IEnumerable<Token> Tokenize(this TextReader reader, string file)
        {
            var buffer = new StringBuilder();

            int lineNumber = 0, tokenIndex = 0;

            char commentType = '\0';
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                int columnNumber = 0, tokenStart = 1;
                char lastChar = '\0', stringType = '\0';
                TokenType type = TokenType.None;
                bool isEscaped = false;

                if (commentType == '/') commentType = '\0'; // line-comments don't survive into subsequent lines

                foreach (char c in line)
                {
                    columnNumber++;

                    if (commentType != '\0')
                    {
                        if (commentType == '*' && c == '/' && buffer.Length != 0 && buffer[buffer.Length - 1] == '*')
                        {
                            // end block comment
                            buffer.Length -= 1; // remove the '*' in '*/'
                            if (buffer.Length != 0)
                            {
                                yield return new Token(buffer.ToString(), lineNumber, tokenStart, TokenType.Comment, line, tokenIndex++, file);
                                buffer.Clear();
                            }

                            commentType = '\0';
                            type = TokenType.None;
                            tokenStart = columnNumber;
                        }
                        else
                        {
                            // otherwise, just accumulate
                            buffer.Append(c);
                        }
                    }
                    else if (type == TokenType.StringLiteral)
                    {
                        if (c == stringType && !isEscaped)
                        {
                            yield return new Token(buffer.ToString(), lineNumber, tokenStart, type, line, tokenIndex++, file);
                            buffer.Clear();
                            type = TokenType.None;
                        }
                        else
                        {
                            buffer.Append(c);
                            isEscaped = !isEscaped && c == '\\'; // ends an existing escape or starts a new one
                        }
                    }
                    else
                    {
                        var newType = Identify(c);
                        if (newType == type && type != TokenType.Symbol)
                        {   // can always append non-symbol types
                            buffer.Append(c);
                        }
                        else if (newType == type && buffer.Length == 1 && lastChar == '/' && c is '/' or '*')
                        {
                            // for symbols, only comments are expected together, and they have special handling
                            // so: we start a comment region using the '/' or '*' to mark the kind
                            buffer.Clear();
                            tokenStart = columnNumber + 1;
                            commentType = c;
                        }
                        else
                        {
                            if (buffer.Length != 0)
                            {
                                yield return new Token(buffer.ToString(), lineNumber, tokenStart, type, line, tokenIndex++, file);
                                buffer.Clear();
                            }
                            type = newType;
                            tokenStart = columnNumber;
                            if (newType == TokenType.StringLiteral)
                            {
                                stringType = c;
                            }
                            else
                            {
                                buffer.Append(c);
                            }
                        }
                    }
                    lastChar = c;
                }

                // process anything left on the line
                if (buffer.Length != 0)
                {
                    if (commentType != '\0') type = TokenType.Comment;
                    yield return new Token(buffer.ToString(), lineNumber, tokenStart, type, line, tokenIndex++, file);
                    buffer.Clear();
                }
            }
        }
        internal static bool TryParseSingle(string token, out float val)
        {
            if (token == "nan")
            {
                val = float.NaN;
                return true;
            }
            if (token == "inf")
            {
                val = float.PositiveInfinity;
                return true;
            }
            if (token == "-inf")
            {
                val = float.NegativeInfinity;
                return true;
            }
            return float.TryParse(token, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out val);
        }
        internal static bool TryParseDouble(string token, out double val)
        {
            if (token == "nan")
            {
                val = double.NaN;
                return true;
            }
            if (token == "inf")
            {
                val = double.PositiveInfinity;
                return true;
            }
            if (token == "-inf")
            {
                val = double.NegativeInfinity;
                return true;
            }
            return double.TryParse(token, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out val);
        }
    }
}

