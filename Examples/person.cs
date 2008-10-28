
// Generated from person.proto
// Option: xml serialization enabled  

namespace person.proto
{

    [System.Serializable, ProtoBuf.ProtoContract(Name = @"Person")]

    [System.Xml.Serialization.XmlType(TypeName = @"Person")]

    public partial class Person
    {
        public Person() { }


        private string _ID0EU;

        [ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"name")]

        [System.Xml.Serialization.XmlElement(@"name", Order = 1)]

        public string name
        {
            get { return _ID0EU; }
            set { _ID0EU = value; }
        }

        private int _ID0E6;

        [ProtoBuf.ProtoMember(2, IsRequired = true, Name = @"id")]

        [System.Xml.Serialization.XmlElement(@"id", Order = 2)]

        public int id
        {
            get { return _ID0E6; }
            set { _ID0E6 = value; }
        }

        private string _ID0EKB = "";

        [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"email")]
        [System.ComponentModel.DefaultValue("")]

        [System.Xml.Serialization.XmlElement(@"email", Order = 3)]

        public string email
        {
            get { return _ID0EKB; }
            set { _ID0EKB = value; }
        }

        private readonly System.Collections.Generic.List<Person.PhoneNumber> _ID0ETB = new System.Collections.Generic.List<Person.PhoneNumber>();

        [ProtoBuf.ProtoMember(4, Name = @"phone")]

        [System.Xml.Serialization.XmlElement(@"phone", Order = 4)]

        public System.Collections.Generic.List<Person.PhoneNumber> phone
        {
            get { return _ID0ETB; }
            set
            { // setter needed for XmlSerializer
                _ID0ETB.Clear();
                if (value != null)
                {
                    _ID0ETB.AddRange(value);
                }
            }
        }

        [System.Serializable, ProtoBuf.ProtoContract(Name = @"PhoneNumber")]

        [System.Xml.Serialization.XmlType(TypeName = @"PhoneNumber")]

        public partial class PhoneNumber
        {
            public PhoneNumber() { }


            private string _ID0ELC;

            [ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"number")]

            [System.Xml.Serialization.XmlElement(@"number", Order = 1)]

            public string number
            {
                get { return _ID0ELC; }
                set { _ID0ELC = value; }
            }

            private Person.PhoneType _ID0EWC = Person.PhoneType.HOME;

            [ProtoBuf.ProtoMember(2, IsRequired = false, Name = @"type")]
            [System.ComponentModel.DefaultValue(Person.PhoneType.HOME)]

            [System.Xml.Serialization.XmlElement(@"type", Order = 2)]

            public Person.PhoneType type
            {
                get { return _ID0EWC; }
                set { _ID0EWC = value; }
            }

        }

        public enum PhoneType
        {
            MOBILE = 0,
            HOME = 1,
            WORK = 2
        }

    }

}
