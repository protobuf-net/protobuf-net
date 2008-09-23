using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ProtoBuf;

namespace QuickStart
{
    static class FileAccess
    {
        /// <summary>
        /// Demonstrates core read/write operations using a file system
        /// as a simple example.
        /// </summary>
        public static void ShowFileAccess()
        {
            string path = Path.GetTempFileName();

            WriteCustomer(path);
            ReadCustomer(path);

            File.Delete(path);
        }

        /// <summary>
        /// Write an object to a stream (in this case, a file)
        /// </summary>
        static void WriteCustomer(string path)
        {
            Customer cust = InventCustomer();

            using (Stream file = File.Create(path))
            {
                Serializer.Serialize(file, cust);
                file.Close();
            }

            Console.WriteLine("{0}: {1} bytes",
                Path.GetFileName(path), new FileInfo(path).Length);         
        }

        /// <summary>
        /// Read an object from a stream (in this case, a file)
        /// </summary>
        static void ReadCustomer(string path)
        {
            Customer cust;
            using (Stream file = File.OpenRead(path))
            {
                cust = Serializer.Deserialize<Customer>(file);
            }

            Console.WriteLine("{0}: {1} ({2} contact(s))",
                cust.CustomerId, cust.Name, cust.Contacts.Count);
        }

        /// <summary>
        /// Returns an invented customer... nothing special
        /// </summary>
        static Customer InventCustomer()
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
}
