using System.Collections.Generic;
using System.Globalization;

namespace ProtoBuf
{
    public class Reservation : ProtoBase
    {
        public List<ReservedBase> Items { get; } = new List<ReservedBase>();
        public abstract class ReservedBase { }
        public sealed class ReservedName : ReservedBase
        {
            public ReservedName(string name) { Name = name; }
            public string Name { get; }
            public override string ToString() => Name;
        }
        public sealed class ReservedNumbers : ReservedBase
        {
            public ReservedNumbers(uint number) : this(number, number) { }
            public ReservedNumbers(uint from, uint to) { From = from; To = to; }
            public uint From { get; }
            public uint To { get; }
            public override string ToString() => From == To ? From.ToString(CultureInfo.InvariantCulture)
                : (From.ToString(CultureInfo.InvariantCulture) + "-" + To.ToString(CultureInfo.InvariantCulture));
        }
        internal static Reservation Parse(Peekable<Token> tokens, ProtoSyntax syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "reserved");
            var token = tokens.Read(); // test the first one to determine what we're doing
            var res = new Reservation();
            switch (token.Type)
            {
                case TokenType.StringLiteral:
                    while (true)
                    {
                        res.Items.Add(new ReservedName(tokens.Consume(TokenType.StringLiteral)));
                        token = tokens.Read();
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
                    break;
                case TokenType.AlphaNumeric:
                    while (true)
                    {
                        uint from = tokens.ConsumeUInt32();
                        if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                        {
                            tokens.Consume();
                            uint to = tokens.ConsumeUInt32();
                            res.Items.Add(new ReservedNumbers(from, to));
                        }
                        else
                        {
                            res.Items.Add(new ReservedNumbers(from));
                        }
                        token = tokens.Read();
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
                    break;
                default:
                    throw token.SyntaxError();
            }
            return res;
        }
    }
}
