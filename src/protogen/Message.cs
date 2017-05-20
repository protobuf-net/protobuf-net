using System.Collections.Generic;
using System.Linq;

namespace ProtoBuf
{
    public class Message : ProtoBase
    {
        public override string ToString() => Name;
        public string Name { get; }
        private Message(string name)
        {
            Name = name;
        }
        internal static Message Parse(Peekable<Token> tokens, ProtoSyntax syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "message");

            string msgName = tokens.Consume(TokenType.AlphaNumeric);
            var message = new Message(msgName);
            tokens.Consume(TokenType.Symbol, "{");
            while (tokens.Peek(out Token token) && !token.Is(TokenType.Symbol, "}"))
            {
                if (ProtoBase.TryParse(tokens, syntax, out var item))
                {
                    message.Items.Add(item);
                }
                else
                {   // assume anything else is a field
                    message.Items.Add(Field.Parse(tokens, syntax));
                }
            }
            tokens.Consume(TokenType.Symbol, "}");
            return message;

        }
        public List<ProtoBase> Items { get; } = new List<ProtoBase>();
        public IEnumerable<Field> Fields => Items.OfType<Field>();
        public IEnumerable<Message> Messages => Items.OfType<Message>();

        public IEnumerable<Reservation> Reservations => Items.OfType<Reservation>();
        public IEnumerable<Enum> Enums => Items.OfType<Enum>();
    }
}
