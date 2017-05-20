using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ProtoBuf
{
    public class Extensions : ProtoBase
    {
        public List<Range> Ranges { get; } = new List<Range>();

        internal static Extensions Parse(Peekable<Token> tokens, ProtoSyntax syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "extensions");
            var res = new Extensions();

            const int MAX = 536870911;
            while (true)
            {
                uint from = tokens.ConsumeUInt32(MAX);
                if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                {
                    tokens.Consume();
                    uint to = tokens.ConsumeUInt32(MAX);
                    res.Ranges.Add(new Range(from, to));
                }
                else
                {
                    res.Ranges.Add(new Range(from));
                }
                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, ","))
                {
                    tokens.Consume();
                }
                else if (token.Is(TokenType.Symbol, ";"))
                {
                    tokens.Consume();
                    break;
                }
                else
                {
                    token.SyntaxError();
                }
            }
            return res;
        }
    }
    public sealed class Range
    {
        public Range(uint number) : this(number, number) { }
        public Range(uint from, uint to) { From = from; To = to; }
        public uint From { get; }
        public uint To { get; }
        public override string ToString() => From == To ? From.ToString(CultureInfo.InvariantCulture)
            : (From.ToString(CultureInfo.InvariantCulture) + "-" + To.ToString(CultureInfo.InvariantCulture));
    }
}
