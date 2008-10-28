
// Generated from person.proto
// Option: xml serialization enabled  

// Option: observable (change notifications) enabled

namespace person.proto
{

    [System.Serializable, ProtoBuf.ProtoContract(Name = @"Person")]

    [System.Xml.Serialization.XmlType(TypeName = @"Person")]

    public partial class Person : ProtoBuf.IExtensible
    , System.ComponentModel.INotifyPropertyChanged
    {
        public Person() { }


        private string _ID0EU;

        [ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"name", DataFormat = ProtoBuf.DataFormat.Default)]

        [System.Xml.Serialization.XmlElement(@"name", Order = 1)]

        public string name
        {
            get { return _ID0EU; }
            set { _ID0EU = value; OnPropertyChanged(@"name"); }
        }

        private int _ID0E6;

        [ProtoBuf.ProtoMember(2, IsRequired = true, Name = @"id", DataFormat = ProtoBuf.DataFormat.TwosComplement)]

        [System.Xml.Serialization.XmlElement(@"id", Order = 2)]

        public int id
        {
            get { return _ID0E6; }
            set { _ID0E6 = value; OnPropertyChanged(@"id"); }
        }

        private string _ID0EKB = "";

        [ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"email", DataFormat = ProtoBuf.DataFormat.Default)]
        [System.ComponentModel.DefaultValue("")]

        [System.Xml.Serialization.XmlElement(@"email", Order = 3)]

        public string email
        {
            get { return _ID0EKB; }
            set { _ID0EKB = value; OnPropertyChanged(@"email"); }
        }

        private readonly System.Collections.Generic.List<Person.PhoneNumber> _ID0ETB = new System.Collections.Generic.List<Person.PhoneNumber>();

        [ProtoBuf.ProtoMember(4, Name = @"phone", DataFormat = ProtoBuf.DataFormat.Default)]

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

        public partial class PhoneNumber : ProtoBuf.IExtensible
        , System.ComponentModel.INotifyPropertyChanged
        {
            public PhoneNumber() { }


            private string _ID0ELC;

            [ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"number", DataFormat = ProtoBuf.DataFormat.Default)]

            [System.Xml.Serialization.XmlElement(@"number", Order = 1)]

            public string number
            {
                get { return _ID0ELC; }
                set { _ID0ELC = value; OnPropertyChanged(@"number"); }
            }

            private Person.PhoneType _ID0EWC = Person.PhoneType.HOME;

            [ProtoBuf.ProtoMember(2, IsRequired = false, Name = @"type", DataFormat = ProtoBuf.DataFormat.TwosComplement)]
            [System.ComponentModel.DefaultValue(Person.PhoneType.HOME)]

            [System.Xml.Serialization.XmlElement(@"type", Order = 2)]

            public Person.PhoneType type
            {
                get { return _ID0EWC; }
                set { _ID0EWC = value; OnPropertyChanged(@"type"); }
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            { if (PropertyChanged != null) PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName)); }

            private ProtoBuf.IExtension extensionObject;
            ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
        }

        public enum PhoneType
        {
            MOBILE = 0,
            HOME = 1,
            WORK = 2
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        { if (PropertyChanged != null) PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName)); }

        private ProtoBuf.IExtension extensionObject;
        ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }

}
