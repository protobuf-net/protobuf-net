using ProtoBuf;
using System.Collections.Generic;
using System;

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

        /// <summary>
        /// Writes details of the current customer to the console for
        /// review purposes.
        /// </summary>
        internal void ShowCustomer()
        {
            Console.WriteLine("{0}: {1} ({2} contact(s))",
                this.CustomerId, this.Name, this.Contacts.Count);
        }

        /// <summary>
        /// Returns an invented customer... nothing special
        /// </summary>
        public static Customer Invent()
        {
            return new Customer
            {
                CustomerId = "abc123",
                MaximumOrderAmount = 123.45M,
                Name = "FooBar Inc.",
                Contacts =
                {
                    new Contact
                    {
                        Name = "Jo",
                        ContactDetails = "jo@foobar.inc"
                    },
                    new Contact
                    {
                        Name = "Fred",
                        ContactDetails = "01234121412"
                    }
                }
            };
        }
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
