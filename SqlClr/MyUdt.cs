using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System;
using ProtoBuf;

namespace SqlClr
{
    [ProtoContract]
    [SqlUserDefinedTypeAttribute(Format.UserDefined, IsByteOrdered=true,
        IsFixedLength = false, MaxByteSize=1024)]
    public sealed class MyProtoUdt : INullable, IBinarySerialize
    {
        public bool IsNull { get { return false; } }
        public static MyProtoUdt Null() { return null; }

        public static MyProtoUdt Parse(string value) {
            throw new NotImplementedException();
        }

        void IBinarySerialize.Read(System.IO.BinaryReader r) {
            Serializer.Merge<MyProtoUdt>(r.BaseStream, this);
        }

        void IBinarySerialize.Write(System.IO.BinaryWriter w) {
            Serializer.Serialize<MyProtoUdt>(w.BaseStream, this);
        }
        [ProtoMember(3)]
        public int ShoeSize { get; set; }
        [ProtoMember(4)]
        public DateTime DateOfBirth { get; set; }
        [ProtoMember(5)]
        public bool IsActive { get; set; }
        [ProtoMember(6)]
        public decimal Balance { get; set; }
        [ProtoMember(7)]
        public float Ratio { get; set; }
    }


    [ProtoContract]
    [SqlUserDefinedTypeAttribute(Format.Native, IsByteOrdered = true)]
    public sealed class MyBasicUdt : INullable
    {
        public bool IsNull { get { return false; } }
        public static MyBasicUdt Null() { return null; }

        public static MyBasicUdt Parse(string value)
        {
            throw new NotImplementedException();
        }

        public int ShoeSize { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
        public float Ratio { get; set; }
    }
}
