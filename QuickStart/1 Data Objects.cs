using ProtoBuf;
using System.Collections.Generic;

namespace QuickStart
{
    [ProtoContract]
    class Customer
    {
        [ProtoMember(1)]
        public string CustomerId { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public decimal MaximumOrderAmount { get; set; }

        private readonly List<Contact> contacts = new List<Contact>();
        [ProtoMember(4)]
        public List<Contact> Contacts { get { return contacts; } }
    }

    [ProtoContract]
    class Contact
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string ContactDetails { get; set; }
    }
}
