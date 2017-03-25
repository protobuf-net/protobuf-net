using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue202
    {
        [ProtoContract]
        class App
        {
            [ProtoMember(1)]
            public List<Bp> list { get; set; }

            public App()
            {
                list = new List<Bp>();
            }
        }

        [ProtoContract]
        class Afp
        {
            [ProtoMember(1)]
            public List<Bp> list;

            public Afp()
            {
                list = new List<Bp>();
            }
        }

        [ProtoContract]
        class Aff
        {
            [ProtoMember(1)]
            public List<Bf> list;

            public Aff()
            {
                list = new List<Bf>();
            }
        }

        [ProtoContract]
        class Apf
        {
            [ProtoMember(1)]
            public List<Bf> list { get; set; }

            public Apf()
            {
                list = new List<Bf>();
            }
        }

        [ProtoContract]
        class Bp
        {
            [ProtoMember(1)]
            public string a { get; set; }

            [ProtoMember(2)]
            public List<string> b { get; set; }

            public Bp()
            {
                b = new List<string>();
            }
        }

        [ProtoContract]
        class Bf
        {
            [ProtoMember(1)]
            public string a;
            [ProtoMember(2)]
            public List<string> b;

            public Bf()
            {
                b = new List<string>();
            }
        }
        [Test]
        public void Execute()
        {
            //Test Data
            App app = new App();
            Apf apf = new Apf();
            Afp afp = new Afp();
            Aff aff = new Aff();
            Bp bp1 = new Bp() { a = "b1" };
            Bp bp2 = new Bp() { a = "b2" };
            Bf bf1 = new Bf() { a = "b1" };
            Bf bf2 = new Bf() { a = "b2" };
            bp1.b = new List<string>();
            bp2.b = new List<string>();
            bf1.b = new List<string>();
            bf2.b = new List<string>();
            bp1.b.Add("a");
            bp2.b.Add("b");
            bf1.b.Add("a");
            bf2.b.Add("b");
            app.list.Add(bp1);
            app.list.Add(bp2);
            apf.list.Add(bf1);
            apf.list.Add(bf2);
            afp.list.Add(bp1);
            afp.list.Add(bp2);
            aff.list.Add(bf1);
            aff.list.Add(bf2);

            StringWriter before = new StringWriter();
            before.WriteLine(Format(app));
            before.WriteLine(Format(apf));
            before.WriteLine(Format(afp));
            before.WriteLine(Format(aff));

            //Serialize
            MemoryStream mspp = new MemoryStream();
            MemoryStream mspf = new MemoryStream();
            MemoryStream msfp = new MemoryStream();
            MemoryStream msff = new MemoryStream();
            Serializer.Serialize(mspp, app);
            Serializer.Serialize(mspf, apf);
            Serializer.Serialize(msfp, afp);
            Serializer.Serialize(msff, aff);

            //Compare binary data
            byte[] bpp = mspp.ToArray();
            byte[] bpf = mspf.ToArray();
            byte[] bfp = msfp.ToArray();
            byte[] bff = msff.ToArray();

            Assert.AreEqual(18, bpp.Length);

            if (bpp.Length != bpf.Length)
                throw new InvalidDataException("Length does not match");
            if (bpf.Length != bff.Length)
                throw new InvalidDataException("Length does not match");
            if (bff.Length != bfp.Length)
                throw new InvalidDataException("Length does not match");
            for (int n = 0; n < bpp.Length; n++)
            {
                if (bpp[n] != bpf[n] || bpf[n] != bff[n] || bff[n] != bfp[n])
                    throw new InvalidDataException("Data does not match");
            }

            //Deserialize
            StringWriter after = new StringWriter();
            Deserialize(bpp, after);

            Assert.AreEqual(before.ToString(), after.ToString());

        }

        public static void Deserialize(byte[] b, TextWriter dest)
        {
            MemoryStream mspp = new MemoryStream(b);
            MemoryStream mspf = new MemoryStream(b);
            MemoryStream msfp = new MemoryStream(b);
            MemoryStream msff = new MemoryStream(b);
            App app = Serializer.Deserialize<App>(mspp);
            Apf apf = Serializer.Deserialize<Apf>(mspf);
            Afp afp = Serializer.Deserialize<Afp>(msfp);
            Aff aff = Serializer.Deserialize<Aff>(msff);

            dest.WriteLine(Format(app));
            dest.WriteLine(Format(apf));
            dest.WriteLine(Format(afp));
            dest.WriteLine(Format(aff));
        }

        private static string Format(App a)
        {
            string s = "[" + a.GetType().Name + "]";
            if (a.list == null)
                return s + " null";
            s += " " + a.list.Count;
            foreach (var b in a.list)
                s += "\n\t" + Format(b);
            return s;
        }

        private static string Format(Afp a)
        {
            string s = "[" + a.GetType().Name + "]";
            if (a.list == null)
                return s + " null";
            s += " " + a.list.Count;
            foreach (var b in a.list)
                s += "\n\t" + Format(b);
            return s;
        }

        private static string Format(Aff a)
        {
            string s = "[" + a.GetType().Name + "]";
            if (a.list == null)
                return s + " null";
            s += " " + a.list.Count;
            foreach (var b in a.list)
                s += "\n\t" + Format(b);
            return s;
        }

        private static string Format(Apf a)
        {
            string s = "[" + a.GetType().Name + "]";
            if (a.list == null)
                return s + " null";
            s += " " + a.list.Count;
            foreach (var b in a.list)
                s += "\n\t" + Format(b);
            return s;
        }

        private static string Format(Bf b)
        {
            string s = "[" + b.GetType().Name + "]";
            s += " " + b.a;
            if (b.b == null)
                return s + " null";
            s += " " + b.b.Count;
            foreach (var i in b.b)
                s += ": " + i;
            return s;
        }

        private static string Format(Bp b)
        {
            string s = "[" + b.GetType().Name + "]";
            s += " " + b.a;
            if (b.b == null)
                return s + " null";
            s += " " + b.b.Count;
            foreach (var i in b.b)
                s += ": " + i;
            return s;
        }
    }
}
