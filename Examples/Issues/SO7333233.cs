
using NUnit.Framework;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    using System.Collections.Generic;
    using System.IO;
    using ProtoBuf;

    [TestFixture]
    public class SO7333233
    {
        [ProtoContract]
        [ProtoInclude(2, typeof(Ant))]
        [ProtoInclude(3, typeof(Cat))]
        public interface IBeast
        {
            [ProtoMember(1)]
            string Name { get; set; }
        }

        [ProtoContract]
        public class Ant : IBeast
        {
            public string Name { get; set; }
        }

        [ProtoContract]
        public class Cat : IBeast
        {
            public string Name { get; set; }
        }

        [ProtoContract]
        public interface IRule<T> where T : IBeast
        {
            bool IsHappy(T beast);
        }

        [ProtoContract]
        public class AntRule1 : IRule<IAnt>, IRule<Ant>
        {
            public bool IsHappy(IAnt beast)
            {
                return true;
            }
            public bool IsHappy(Ant beast)
            {
                return true;
            }
        }

        [ProtoContract]
        public class AntRule2 : IRule<IAnt>, IRule<Ant>
        {
            public bool IsHappy(IAnt beast)
            {
                return true;
            }
            public bool IsHappy(Ant beast)
            {
                return true;
            }
        }

        public interface ICat : IBeast
        {
        }

        public interface IAnt : IBeast
        {
        }


        [ProtoContract]
        public class CatRule1 : IRule<ICat>, IRule<Cat>
        {
            public bool IsHappy(ICat beast)
            {
                return true;
            }
            public bool IsHappy(Cat beast)
            {
                return true;
            }
        }

        [ProtoContract]
        public class CatRule2 : IRule<ICat>, IRule<Cat>
        {
            public bool IsHappy(ICat beast)
            {
                return true;
            }
            public bool IsHappy(Cat beast)
            {
                return true;
            }
        }

        [Test]
        public  void Execute()
        {
            // note these are unrelated networks, so we can use the same field-numbers
            RuntimeTypeModel.Default[typeof(IRule<Ant>)].AddSubType(1, typeof(AntRule1)).AddSubType(2, typeof(AntRule2));
            RuntimeTypeModel.Default[typeof(IRule<Cat>)].AddSubType(1, typeof(CatRule1)).AddSubType(2, typeof(CatRule2));

            var antRules = new List<IRule<Ant>>();
            antRules.Add(new AntRule1());
            antRules.Add(new AntRule2());

            var catRules = new List<IRule<Cat>>();
            catRules.Add(new CatRule1());
            catRules.Add(new CatRule2());

            using (var fs = File.Create(@"antRules.bin"))
            {
                ProtoBuf.Serializer.Serialize(fs, antRules);

                fs.Close();
            }

            using (var fs = File.OpenRead(@"antRules.bin"))
            {
                List<IRule<Ant>> list;
                list = ProtoBuf.Serializer.Deserialize<List<IRule<Ant>>>(fs);

                fs.Close();
            }

            using (var fs = File.Create(@"catRules.bin"))
            {
                ProtoBuf.Serializer.Serialize(fs, catRules);

                fs.Close();
            }

            using (var fs = File.OpenRead(@"catRules.bin"))
            {
                List<IRule<Cat>> list;
                list = ProtoBuf.Serializer.Deserialize<List<IRule<Cat>>>(fs);

                fs.Close();
            }
        }
    }
}
