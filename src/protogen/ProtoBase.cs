namespace ProtoBuf
{
    public abstract class ProtoBase
    {

        internal static bool TryParse(Peekable<Token> tokens, ProtoSyntax syntax, out ProtoBase item)
        {
            if (tokens.Peek(out var token) && token.Is(TokenType.AlphaNumeric))
            {
                switch (token.Value)
                {
                    case "message":
                        item = Message.Parse(tokens, syntax);
                        return true;
                    case "reserved":
                        item = Reservation.Parse(tokens, syntax);
                        return true;
                    case "extensions":
                        item = Extensions.Parse(tokens, syntax);
                        return true;
                    case "enum":
                        item = Enum.TryParse(tokens, syntax);
                        return true;
                }
            }
            item = default(ProtoBase);
            return false;
        }


    }
}
