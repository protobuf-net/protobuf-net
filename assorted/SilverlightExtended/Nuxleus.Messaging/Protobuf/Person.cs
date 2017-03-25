using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using ProtoBuf;
using System.Collections.Generic;

namespace Nuxleus.Messaging.Protobuf {

    [ProtoContract]
    public class Person {
        [ProtoMember(1, Name = "Name", IsRequired = true)]
        public string Name { get; set; }
        [ProtoMember(2, Name = "ID", IsRequired = true, DataFormat = DataFormat.TwosComplement)]
        public int ID { get; set; }
        [ProtoMember(3, Name = "Email", IsRequired = true)]
        public string Email { get; set; }
        [ProtoMember(4, Name = "Phone", IsRequired = false)]
        public List<PhoneNumber> Phone { get; set; } 
    }

    [ProtoContract]
    public class PhoneNumber {
        [ProtoMember(1, Name = "Number", IsRequired = true)]
        public string Number { get; set; }

        [DefaultValue(PhoneType.HOME)]
        [ProtoMember(2, Name = "Type", IsRequired = true)]
        public PhoneType Type { get; set; }
    }

    [ProtoContract]
    public enum PhoneType { MOBILE, HOME, WORK }
}
