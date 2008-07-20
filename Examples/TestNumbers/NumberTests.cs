using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.Runtime.Serialization;
using System.IO;

namespace Examples.TestNumbers
{
    [ProtoContract]
    class NumRig
    {
        [ProtoMember(1, DataFormat=DataFormat.Default)]
        public int Int32Default { get; set; }
        [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
        public int Int32ZigZag { get; set; }
        [ProtoMember(3, DataFormat = DataFormat.TwosComplement)]
        public int Int32TwosComplement { get; set; }
        [ProtoMember(4, DataFormat = DataFormat.FixedSize)]
        public int Int32FixedSize { get; set; }

        [ProtoMember(5, DataFormat = DataFormat.Default)]
        public uint UInt32Default { get; set; }
        //[ProtoMember(6, DataFormat = DataFormat.ZigZag)]
        //public uint UInt32ZigZag { get; set; }
        //[ProtoMember(7, DataFormat = DataFormat.TwosComplement)]
        //public uint UInt32TwosComplement { get; set; }
        //[ProtoMember(8, DataFormat = DataFormat.FixedSize)]
        //public uint UInt32FixedSize { get; set; }

        [ProtoMember(9, DataFormat = DataFormat.Default)]
        public long Int64Default { get; set; }
        [ProtoMember(10, DataFormat = DataFormat.ZigZag)]
        public long Int64ZigZag { get; set; }
        [ProtoMember(11, DataFormat = DataFormat.TwosComplement)]
        public long Int64TwosComplement { get; set; }
        [ProtoMember(12, DataFormat = DataFormat.FixedSize)]
        public long Int64FixedSize { get; set; }

        [ProtoMember(13, DataFormat = DataFormat.Default)]
        public ulong UInt64Default { get; set; }
        //[ProtoMember(14, DataFormat = DataFormat.ZigZag)]
        //public ulong UInt64ZigZag { get; set; }
        //[ProtoMember(15, DataFormat = DataFormat.TwosComplement)]
        //public ulong UInt64TwosComplement { get; set; }
        //[ProtoMember(16, DataFormat = DataFormat.FixedSize)]
        //public ulong UInt64FixedSize { get; set; }
        
        [ProtoMember(17)]
        public string Foo { get; set; }
    }

    [ProtoContract]
    class ZigZagInt32
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        public int Foo { get; set; }
    }
    [ProtoContract]
    class TwosComplementInt32
    {
        [ProtoMember(1, DataFormat = DataFormat.TwosComplement)]
        public int Foo { get; set; }
    }

    class SignTests
    {
        public static void Run(int index)
        {
            CheckBytes(new TwosComplementInt32 { Foo = 1 }, 0x08, 0x01);
            CheckBytes(new TwosComplementInt32 { Foo = 2 }, 0x08, 0x02);
            CheckBytes(new TwosComplementInt32 { Foo = -1 }, 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01);
            CheckBytes(new TwosComplementInt32 { Foo = -2 }, 0x08, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01);

            CheckBytes(new ZigZagInt32 { Foo = 1 }, 0x08, 0x02);
            CheckBytes(new ZigZagInt32 { Foo = 2 }, 0x08, 0x04);
            CheckBytes(new ZigZagInt32 { Foo = -1 }, 0x08, 0x01);
            CheckBytes(new ZigZagInt32 { Foo = -2 }, 0x08, 0x03);
        }
        private static void CheckBytes<T>(T item, params byte[] expected) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, item);
                byte[] actual = ms.ToArray();
                bool match = actual.Length == expected.Length;
                if (match)
                {
                    for (int i = 0; i < expected.Length; i++)
                    {
                        match &= actual[i] == expected[i];
                    }
                }
                if (!match)
                {
                    Console.WriteLine("Hi");
                }
                Console.WriteLine("\tMatch ({0}): {1}", typeof(T).Name, match ? "Pass" : "Fail");
            }
        }
    }
    class NumberTests
    {
        

        public static void Run(int index)
        {
            NumRig rig = new NumRig();
            const string SUCCESS = "bar";
            rig.Foo = SUCCESS; // to help test stream ending prematurely
            int count = 0;
            for (int i = 0; i < 32; i++)
            {
                int bigBit = i == 0 ? 0 : (1 << i - 1);
                for (int j = 0; j <= i; j++)
                {
                    int smallBit = 1 << j;
                    int val = bigBit | smallBit;
                    rig.Int32Default
                        = rig.Int32FixedSize
                        = rig.Int32TwosComplement
                        = rig.Int32ZigZag
                        = val;
                    try
                    {
                        NumRig clone = Serializer.DeepClone(rig);
                        if (rig.Int32Default != val) Console.WriteLine("Int32Default failed for: {0}", val);
                        if (rig.Int32FixedSize != val) Console.WriteLine("Int32FixedSize failed for: {0}", val);
                        if (rig.Int32TwosComplement != val) Console.WriteLine("Int32TwosComplement failed for: {0}", val);
                        if (rig.Int32ZigZag != val) Console.WriteLine("Int32ZigZag failed for: {0}", val);
                        if (rig.Foo != SUCCESS) Console.WriteLine("Unknown serialization error for: {0}", val);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}, {1} = {2}", i,j,val);
                        while (ex != null)
                        {
                            Console.WriteLine(ex.Message);
                            ex = ex.InnerException;
                        }
                    }
                }
            }
            Console.WriteLine("\tInt32 tests: {0}", count);
            count = 0;
            for (int i = 0; i < 64; i++)
            {
                long bigBit = i == 0 ? 0 : (1 << i - 1);
                for (int j = 0; j <= i; j++)
                {
                    long smallBit = 1 << j;
                    long val = bigBit | smallBit;
                    rig.Int64Default
                        = rig.Int64FixedSize
                        = rig.Int64TwosComplement
                        = rig.Int64ZigZag
                        = val;
                    try
                    {
                        NumRig clone = Serializer.DeepClone(rig);
                        if (rig.Int64Default != val) Console.WriteLine("Int64Default failed for: {0}", val);
                        if (rig.Int64FixedSize != val) Console.WriteLine("Int64FixedSize failed for: {0}", val);
                        if (rig.Int64TwosComplement != val) Console.WriteLine("Int64TwosComplement failed for: {0}", val);
                        if (rig.Int64ZigZag != val) Console.WriteLine("Int64ZigZag failed for: {0}", val);
                        if (rig.Foo != SUCCESS) Console.WriteLine("Unknown serialization error for: {0}", val);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}, {1} = {2}", i, j, val);
                        while (ex != null)
                        {
                            Console.WriteLine(ex.Message);
                            ex = ex.InnerException;
                        }
                    }
                }
            }
            Console.WriteLine("\tInt64 tests: {0}", count);
        }
    }
}
