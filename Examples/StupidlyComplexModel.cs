using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.Linq;
namespace Examples
{
    [TestFixture]
    public class StupidlyComplexModel
    {
        [Test]
        public void TimeStupidlyComplexModel()
        {
            TimeModel<StupidlyComplexModel>(5, Test);
        }
        [Test]
        public void TimeSimpleModel()
        {
            TimeModel<SimpleModel>(100);
        }

        [ProtoContract]
        public class SimpleModel
        {
            [ProtoMember(1)] public int A {get;set;}
            [ProtoMember(2)] public float B {get;set;}
            [ProtoMember(3)] public decimal C {get;set;}
            [ProtoMember(4)] public bool D {get;set;}
            [ProtoMember(5)] public byte E {get;set;}
            [ProtoMember(6)] public long F {get;set;}
            [ProtoMember(7)] public short G {get;set;}
            [ProtoMember(8)] public double H {get;set;}
            [ProtoMember(9)] public float I {get;set;}
            [ProtoMember(10)] public uint J {get;set;}
            [ProtoMember(11)] public ulong K {get;set;}
            [ProtoMember(12)] public ushort L {get;set;}
            [ProtoMember(13)] public sbyte M {get;set;}
            [ProtoMember(14)] public DateTime N {get;set;}
            [ProtoMember(15)] public string O {get;set;}
            [ProtoMember(16)] public Type P {get;set;}
            [ProtoMember(17)] public byte[] Q {get;set;}
            [ProtoMember(18)] public SimpleModel R {get;set;}
            [ProtoMember(19)] public TimeSpan S {get;set;}
            [ProtoMember(20)] public int T {get;set;}
        }

        private static void TimeModel<T>(int count, Action<TypeModel, string> test = null)
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(T), true);
            if (test != null) test(model, "Time");
            model.Compile(); // do discovery etc
            int typeCount = model.GetTypes().Cast<MetaType>().Count();

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                model.Compile();
            }
            watch.Stop();
            Console.WriteLine(string.Format("{0}: {1}ms/Compile, {2} types, {3}ms total, {4} iteratons",
                typeof(T).Name, watch.ElapsedMilliseconds / count, typeCount, watch.ElapsedMilliseconds, count));
            
        }


        [Test]
        public void TestStupidlyComplexModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Outer), true);

            Test(model, "Runtime");
            model.CompileInPlace();
            Test(model, "CompileInPlace");
            Test(model.Compile(), "Compile");

            model.Compile("TestStupidlyComplexModel", "TestStupidlyComplexModel.dll");
            PEVerify.AssertValid("TestStupidlyComplexModel.dll");
        }

        private void Test(TypeModel model, string test)
        {
            var orig = new Outer {
                Value500 = new Inner500 { Value = 123 },
                Value501 = new Inner501 { Value = 456 }
            };
            var clone = (Outer)model.DeepClone(orig);
            Assert.AreNotSame(orig, clone, test);
            
            var props = typeof(Outer).GetProperties();
            foreach (var prop in props)
            {
                switch(prop.Name)
                {
                    case "Value500":
                    case "Value501":
                        Assert.IsNotNull(prop.GetValue(orig), test + ":orig:" + prop.Name);
                        Assert.IsNotNull(prop.GetValue(clone), test + ":clone:" + prop.Name);
                        break;
                    default:
                        Assert.IsNull(prop.GetValue(orig), test + ":orig:" + prop.Name);
                        Assert.IsNull(prop.GetValue(clone), test + ":clone:" + prop.Name);
                    break;
                }
            }

            Assert.AreEqual(123, orig.Value500.Value, test + ":orig:Value500.Value");
            Assert.AreEqual(123, clone.Value500.Value, test + ":clone:Value500.Value");
            Assert.AreEqual(456, orig.Value501.Value, test + ":orig:Value501.Value");
            Assert.AreEqual(456, clone.Value501.Value, test + ":clone:Value501.Value");
          
            var clone500 = (Inner500)model.DeepClone(orig.Value500);
            var clone501 = (Inner501)model.DeepClone(orig.Value501);

            Assert.AreEqual(123, clone500.Value, test + ":clone500.Value");
            Assert.AreEqual(456, clone501.Value, test + ":clone501.Value");
          
        }
        [ProtoContract]
        public class Outer
        {
            [ProtoMember(1)]
            public Inner1 Value1 { get; set; }
            [ProtoMember(2)]
            public Inner2 Value2 { get; set; }
            [ProtoMember(3)]
            public Inner3 Value3 { get; set; }
            [ProtoMember(4)]
            public Inner4 Value4 { get; set; }
            [ProtoMember(5)]
            public Inner5 Value5 { get; set; }
            [ProtoMember(6)]
            public Inner6 Value6 { get; set; }
            [ProtoMember(7)]
            public Inner7 Value7 { get; set; }
            [ProtoMember(8)]
            public Inner8 Value8 { get; set; }
            [ProtoMember(9)]
            public Inner9 Value9 { get; set; }
            [ProtoMember(10)]
            public Inner10 Value10 { get; set; }
            [ProtoMember(11)]
            public Inner11 Value11 { get; set; }
            [ProtoMember(12)]
            public Inner12 Value12 { get; set; }
            [ProtoMember(13)]
            public Inner13 Value13 { get; set; }
            [ProtoMember(14)]
            public Inner14 Value14 { get; set; }
            [ProtoMember(15)]
            public Inner15 Value15 { get; set; }
            [ProtoMember(16)]
            public Inner16 Value16 { get; set; }
            [ProtoMember(17)]
            public Inner17 Value17 { get; set; }
            [ProtoMember(18)]
            public Inner18 Value18 { get; set; }
            [ProtoMember(19)]
            public Inner19 Value19 { get; set; }
            [ProtoMember(20)]
            public Inner20 Value20 { get; set; }
            [ProtoMember(21)]
            public Inner21 Value21 { get; set; }
            [ProtoMember(22)]
            public Inner22 Value22 { get; set; }
            [ProtoMember(23)]
            public Inner23 Value23 { get; set; }
            [ProtoMember(24)]
            public Inner24 Value24 { get; set; }
            [ProtoMember(25)]
            public Inner25 Value25 { get; set; }
            [ProtoMember(26)]
            public Inner26 Value26 { get; set; }
            [ProtoMember(27)]
            public Inner27 Value27 { get; set; }
            [ProtoMember(28)]
            public Inner28 Value28 { get; set; }
            [ProtoMember(29)]
            public Inner29 Value29 { get; set; }
            [ProtoMember(30)]
            public Inner30 Value30 { get; set; }
            [ProtoMember(31)]
            public Inner31 Value31 { get; set; }
            [ProtoMember(32)]
            public Inner32 Value32 { get; set; }
            [ProtoMember(33)]
            public Inner33 Value33 { get; set; }
            [ProtoMember(34)]
            public Inner34 Value34 { get; set; }
            [ProtoMember(35)]
            public Inner35 Value35 { get; set; }
            [ProtoMember(36)]
            public Inner36 Value36 { get; set; }
            [ProtoMember(37)]
            public Inner37 Value37 { get; set; }
            [ProtoMember(38)]
            public Inner38 Value38 { get; set; }
            [ProtoMember(39)]
            public Inner39 Value39 { get; set; }
            [ProtoMember(40)]
            public Inner40 Value40 { get; set; }
            [ProtoMember(41)]
            public Inner41 Value41 { get; set; }
            [ProtoMember(42)]
            public Inner42 Value42 { get; set; }
            [ProtoMember(43)]
            public Inner43 Value43 { get; set; }
            [ProtoMember(44)]
            public Inner44 Value44 { get; set; }
            [ProtoMember(45)]
            public Inner45 Value45 { get; set; }
            [ProtoMember(46)]
            public Inner46 Value46 { get; set; }
            [ProtoMember(47)]
            public Inner47 Value47 { get; set; }
            [ProtoMember(48)]
            public Inner48 Value48 { get; set; }
            [ProtoMember(49)]
            public Inner49 Value49 { get; set; }
            [ProtoMember(50)]
            public Inner50 Value50 { get; set; }
            [ProtoMember(51)]
            public Inner51 Value51 { get; set; }
            [ProtoMember(52)]
            public Inner52 Value52 { get; set; }
            [ProtoMember(53)]
            public Inner53 Value53 { get; set; }
            [ProtoMember(54)]
            public Inner54 Value54 { get; set; }
            [ProtoMember(55)]
            public Inner55 Value55 { get; set; }
            [ProtoMember(56)]
            public Inner56 Value56 { get; set; }
            [ProtoMember(57)]
            public Inner57 Value57 { get; set; }
            [ProtoMember(58)]
            public Inner58 Value58 { get; set; }
            [ProtoMember(59)]
            public Inner59 Value59 { get; set; }
            [ProtoMember(60)]
            public Inner60 Value60 { get; set; }
            [ProtoMember(61)]
            public Inner61 Value61 { get; set; }
            [ProtoMember(62)]
            public Inner62 Value62 { get; set; }
            [ProtoMember(63)]
            public Inner63 Value63 { get; set; }
            [ProtoMember(64)]
            public Inner64 Value64 { get; set; }
            [ProtoMember(65)]
            public Inner65 Value65 { get; set; }
            [ProtoMember(66)]
            public Inner66 Value66 { get; set; }
            [ProtoMember(67)]
            public Inner67 Value67 { get; set; }
            [ProtoMember(68)]
            public Inner68 Value68 { get; set; }
            [ProtoMember(69)]
            public Inner69 Value69 { get; set; }
            [ProtoMember(70)]
            public Inner70 Value70 { get; set; }
            [ProtoMember(71)]
            public Inner71 Value71 { get; set; }
            [ProtoMember(72)]
            public Inner72 Value72 { get; set; }
            [ProtoMember(73)]
            public Inner73 Value73 { get; set; }
            [ProtoMember(74)]
            public Inner74 Value74 { get; set; }
            [ProtoMember(75)]
            public Inner75 Value75 { get; set; }
            [ProtoMember(76)]
            public Inner76 Value76 { get; set; }
            [ProtoMember(77)]
            public Inner77 Value77 { get; set; }
            [ProtoMember(78)]
            public Inner78 Value78 { get; set; }
            [ProtoMember(79)]
            public Inner79 Value79 { get; set; }
            [ProtoMember(80)]
            public Inner80 Value80 { get; set; }
            [ProtoMember(81)]
            public Inner81 Value81 { get; set; }
            [ProtoMember(82)]
            public Inner82 Value82 { get; set; }
            [ProtoMember(83)]
            public Inner83 Value83 { get; set; }
            [ProtoMember(84)]
            public Inner84 Value84 { get; set; }
            [ProtoMember(85)]
            public Inner85 Value85 { get; set; }
            [ProtoMember(86)]
            public Inner86 Value86 { get; set; }
            [ProtoMember(87)]
            public Inner87 Value87 { get; set; }
            [ProtoMember(88)]
            public Inner88 Value88 { get; set; }
            [ProtoMember(89)]
            public Inner89 Value89 { get; set; }
            [ProtoMember(90)]
            public Inner90 Value90 { get; set; }
            [ProtoMember(91)]
            public Inner91 Value91 { get; set; }
            [ProtoMember(92)]
            public Inner92 Value92 { get; set; }
            [ProtoMember(93)]
            public Inner93 Value93 { get; set; }
            [ProtoMember(94)]
            public Inner94 Value94 { get; set; }
            [ProtoMember(95)]
            public Inner95 Value95 { get; set; }
            [ProtoMember(96)]
            public Inner96 Value96 { get; set; }
            [ProtoMember(97)]
            public Inner97 Value97 { get; set; }
            [ProtoMember(98)]
            public Inner98 Value98 { get; set; }
            [ProtoMember(99)]
            public Inner99 Value99 { get; set; }
            [ProtoMember(100)]
            public Inner100 Value100 { get; set; }
            [ProtoMember(101)]
            public Inner101 Value101 { get; set; }
            [ProtoMember(102)]
            public Inner102 Value102 { get; set; }
            [ProtoMember(103)]
            public Inner103 Value103 { get; set; }
            [ProtoMember(104)]
            public Inner104 Value104 { get; set; }
            [ProtoMember(105)]
            public Inner105 Value105 { get; set; }
            [ProtoMember(106)]
            public Inner106 Value106 { get; set; }
            [ProtoMember(107)]
            public Inner107 Value107 { get; set; }
            [ProtoMember(108)]
            public Inner108 Value108 { get; set; }
            [ProtoMember(109)]
            public Inner109 Value109 { get; set; }
            [ProtoMember(110)]
            public Inner110 Value110 { get; set; }
            [ProtoMember(111)]
            public Inner111 Value111 { get; set; }
            [ProtoMember(112)]
            public Inner112 Value112 { get; set; }
            [ProtoMember(113)]
            public Inner113 Value113 { get; set; }
            [ProtoMember(114)]
            public Inner114 Value114 { get; set; }
            [ProtoMember(115)]
            public Inner115 Value115 { get; set; }
            [ProtoMember(116)]
            public Inner116 Value116 { get; set; }
            [ProtoMember(117)]
            public Inner117 Value117 { get; set; }
            [ProtoMember(118)]
            public Inner118 Value118 { get; set; }
            [ProtoMember(119)]
            public Inner119 Value119 { get; set; }
            [ProtoMember(120)]
            public Inner120 Value120 { get; set; }
            [ProtoMember(121)]
            public Inner121 Value121 { get; set; }
            [ProtoMember(122)]
            public Inner122 Value122 { get; set; }
            [ProtoMember(123)]
            public Inner123 Value123 { get; set; }
            [ProtoMember(124)]
            public Inner124 Value124 { get; set; }
            [ProtoMember(125)]
            public Inner125 Value125 { get; set; }
            [ProtoMember(126)]
            public Inner126 Value126 { get; set; }
            [ProtoMember(127)]
            public Inner127 Value127 { get; set; }
            [ProtoMember(128)]
            public Inner128 Value128 { get; set; }
            [ProtoMember(129)]
            public Inner129 Value129 { get; set; }
            [ProtoMember(130)]
            public Inner130 Value130 { get; set; }
            [ProtoMember(131)]
            public Inner131 Value131 { get; set; }
            [ProtoMember(132)]
            public Inner132 Value132 { get; set; }
            [ProtoMember(133)]
            public Inner133 Value133 { get; set; }
            [ProtoMember(134)]
            public Inner134 Value134 { get; set; }
            [ProtoMember(135)]
            public Inner135 Value135 { get; set; }
            [ProtoMember(136)]
            public Inner136 Value136 { get; set; }
            [ProtoMember(137)]
            public Inner137 Value137 { get; set; }
            [ProtoMember(138)]
            public Inner138 Value138 { get; set; }
            [ProtoMember(139)]
            public Inner139 Value139 { get; set; }
            [ProtoMember(140)]
            public Inner140 Value140 { get; set; }
            [ProtoMember(141)]
            public Inner141 Value141 { get; set; }
            [ProtoMember(142)]
            public Inner142 Value142 { get; set; }
            [ProtoMember(143)]
            public Inner143 Value143 { get; set; }
            [ProtoMember(144)]
            public Inner144 Value144 { get; set; }
            [ProtoMember(145)]
            public Inner145 Value145 { get; set; }
            [ProtoMember(146)]
            public Inner146 Value146 { get; set; }
            [ProtoMember(147)]
            public Inner147 Value147 { get; set; }
            [ProtoMember(148)]
            public Inner148 Value148 { get; set; }
            [ProtoMember(149)]
            public Inner149 Value149 { get; set; }
            [ProtoMember(150)]
            public Inner150 Value150 { get; set; }
            [ProtoMember(151)]
            public Inner151 Value151 { get; set; }
            [ProtoMember(152)]
            public Inner152 Value152 { get; set; }
            [ProtoMember(153)]
            public Inner153 Value153 { get; set; }
            [ProtoMember(154)]
            public Inner154 Value154 { get; set; }
            [ProtoMember(155)]
            public Inner155 Value155 { get; set; }
            [ProtoMember(156)]
            public Inner156 Value156 { get; set; }
            [ProtoMember(157)]
            public Inner157 Value157 { get; set; }
            [ProtoMember(158)]
            public Inner158 Value158 { get; set; }
            [ProtoMember(159)]
            public Inner159 Value159 { get; set; }
            [ProtoMember(160)]
            public Inner160 Value160 { get; set; }
            [ProtoMember(161)]
            public Inner161 Value161 { get; set; }
            [ProtoMember(162)]
            public Inner162 Value162 { get; set; }
            [ProtoMember(163)]
            public Inner163 Value163 { get; set; }
            [ProtoMember(164)]
            public Inner164 Value164 { get; set; }
            [ProtoMember(165)]
            public Inner165 Value165 { get; set; }
            [ProtoMember(166)]
            public Inner166 Value166 { get; set; }
            [ProtoMember(167)]
            public Inner167 Value167 { get; set; }
            [ProtoMember(168)]
            public Inner168 Value168 { get; set; }
            [ProtoMember(169)]
            public Inner169 Value169 { get; set; }
            [ProtoMember(170)]
            public Inner170 Value170 { get; set; }
            [ProtoMember(171)]
            public Inner171 Value171 { get; set; }
            [ProtoMember(172)]
            public Inner172 Value172 { get; set; }
            [ProtoMember(173)]
            public Inner173 Value173 { get; set; }
            [ProtoMember(174)]
            public Inner174 Value174 { get; set; }
            [ProtoMember(175)]
            public Inner175 Value175 { get; set; }
            [ProtoMember(176)]
            public Inner176 Value176 { get; set; }
            [ProtoMember(177)]
            public Inner177 Value177 { get; set; }
            [ProtoMember(178)]
            public Inner178 Value178 { get; set; }
            [ProtoMember(179)]
            public Inner179 Value179 { get; set; }
            [ProtoMember(180)]
            public Inner180 Value180 { get; set; }
            [ProtoMember(181)]
            public Inner181 Value181 { get; set; }
            [ProtoMember(182)]
            public Inner182 Value182 { get; set; }
            [ProtoMember(183)]
            public Inner183 Value183 { get; set; }
            [ProtoMember(184)]
            public Inner184 Value184 { get; set; }
            [ProtoMember(185)]
            public Inner185 Value185 { get; set; }
            [ProtoMember(186)]
            public Inner186 Value186 { get; set; }
            [ProtoMember(187)]
            public Inner187 Value187 { get; set; }
            [ProtoMember(188)]
            public Inner188 Value188 { get; set; }
            [ProtoMember(189)]
            public Inner189 Value189 { get; set; }
            [ProtoMember(190)]
            public Inner190 Value190 { get; set; }
            [ProtoMember(191)]
            public Inner191 Value191 { get; set; }
            [ProtoMember(192)]
            public Inner192 Value192 { get; set; }
            [ProtoMember(193)]
            public Inner193 Value193 { get; set; }
            [ProtoMember(194)]
            public Inner194 Value194 { get; set; }
            [ProtoMember(195)]
            public Inner195 Value195 { get; set; }
            [ProtoMember(196)]
            public Inner196 Value196 { get; set; }
            [ProtoMember(197)]
            public Inner197 Value197 { get; set; }
            [ProtoMember(198)]
            public Inner198 Value198 { get; set; }
            [ProtoMember(199)]
            public Inner199 Value199 { get; set; }
            [ProtoMember(200)]
            public Inner200 Value200 { get; set; }
            [ProtoMember(201)]
            public Inner201 Value201 { get; set; }
            [ProtoMember(202)]
            public Inner202 Value202 { get; set; }
            [ProtoMember(203)]
            public Inner203 Value203 { get; set; }
            [ProtoMember(204)]
            public Inner204 Value204 { get; set; }
            [ProtoMember(205)]
            public Inner205 Value205 { get; set; }
            [ProtoMember(206)]
            public Inner206 Value206 { get; set; }
            [ProtoMember(207)]
            public Inner207 Value207 { get; set; }
            [ProtoMember(208)]
            public Inner208 Value208 { get; set; }
            [ProtoMember(209)]
            public Inner209 Value209 { get; set; }
            [ProtoMember(210)]
            public Inner210 Value210 { get; set; }
            [ProtoMember(211)]
            public Inner211 Value211 { get; set; }
            [ProtoMember(212)]
            public Inner212 Value212 { get; set; }
            [ProtoMember(213)]
            public Inner213 Value213 { get; set; }
            [ProtoMember(214)]
            public Inner214 Value214 { get; set; }
            [ProtoMember(215)]
            public Inner215 Value215 { get; set; }
            [ProtoMember(216)]
            public Inner216 Value216 { get; set; }
            [ProtoMember(217)]
            public Inner217 Value217 { get; set; }
            [ProtoMember(218)]
            public Inner218 Value218 { get; set; }
            [ProtoMember(219)]
            public Inner219 Value219 { get; set; }
            [ProtoMember(220)]
            public Inner220 Value220 { get; set; }
            [ProtoMember(221)]
            public Inner221 Value221 { get; set; }
            [ProtoMember(222)]
            public Inner222 Value222 { get; set; }
            [ProtoMember(223)]
            public Inner223 Value223 { get; set; }
            [ProtoMember(224)]
            public Inner224 Value224 { get; set; }
            [ProtoMember(225)]
            public Inner225 Value225 { get; set; }
            [ProtoMember(226)]
            public Inner226 Value226 { get; set; }
            [ProtoMember(227)]
            public Inner227 Value227 { get; set; }
            [ProtoMember(228)]
            public Inner228 Value228 { get; set; }
            [ProtoMember(229)]
            public Inner229 Value229 { get; set; }
            [ProtoMember(230)]
            public Inner230 Value230 { get; set; }
            [ProtoMember(231)]
            public Inner231 Value231 { get; set; }
            [ProtoMember(232)]
            public Inner232 Value232 { get; set; }
            [ProtoMember(233)]
            public Inner233 Value233 { get; set; }
            [ProtoMember(234)]
            public Inner234 Value234 { get; set; }
            [ProtoMember(235)]
            public Inner235 Value235 { get; set; }
            [ProtoMember(236)]
            public Inner236 Value236 { get; set; }
            [ProtoMember(237)]
            public Inner237 Value237 { get; set; }
            [ProtoMember(238)]
            public Inner238 Value238 { get; set; }
            [ProtoMember(239)]
            public Inner239 Value239 { get; set; }
            [ProtoMember(240)]
            public Inner240 Value240 { get; set; }
            [ProtoMember(241)]
            public Inner241 Value241 { get; set; }
            [ProtoMember(242)]
            public Inner242 Value242 { get; set; }
            [ProtoMember(243)]
            public Inner243 Value243 { get; set; }
            [ProtoMember(244)]
            public Inner244 Value244 { get; set; }
            [ProtoMember(245)]
            public Inner245 Value245 { get; set; }
            [ProtoMember(246)]
            public Inner246 Value246 { get; set; }
            [ProtoMember(247)]
            public Inner247 Value247 { get; set; }
            [ProtoMember(248)]
            public Inner248 Value248 { get; set; }
            [ProtoMember(249)]
            public Inner249 Value249 { get; set; }
            [ProtoMember(250)]
            public Inner250 Value250 { get; set; }
            [ProtoMember(251)]
            public Inner251 Value251 { get; set; }
            [ProtoMember(252)]
            public Inner252 Value252 { get; set; }
            [ProtoMember(253)]
            public Inner253 Value253 { get; set; }
            [ProtoMember(254)]
            public Inner254 Value254 { get; set; }
            [ProtoMember(255)]
            public Inner255 Value255 { get; set; }
            [ProtoMember(256)]
            public Inner256 Value256 { get; set; }
            [ProtoMember(257)]
            public Inner257 Value257 { get; set; }
            [ProtoMember(258)]
            public Inner258 Value258 { get; set; }
            [ProtoMember(259)]
            public Inner259 Value259 { get; set; }
            [ProtoMember(260)]
            public Inner260 Value260 { get; set; }
            [ProtoMember(261)]
            public Inner261 Value261 { get; set; }
            [ProtoMember(262)]
            public Inner262 Value262 { get; set; }
            [ProtoMember(263)]
            public Inner263 Value263 { get; set; }
            [ProtoMember(264)]
            public Inner264 Value264 { get; set; }
            [ProtoMember(265)]
            public Inner265 Value265 { get; set; }
            [ProtoMember(266)]
            public Inner266 Value266 { get; set; }
            [ProtoMember(267)]
            public Inner267 Value267 { get; set; }
            [ProtoMember(268)]
            public Inner268 Value268 { get; set; }
            [ProtoMember(269)]
            public Inner269 Value269 { get; set; }
            [ProtoMember(270)]
            public Inner270 Value270 { get; set; }
            [ProtoMember(271)]
            public Inner271 Value271 { get; set; }
            [ProtoMember(272)]
            public Inner272 Value272 { get; set; }
            [ProtoMember(273)]
            public Inner273 Value273 { get; set; }
            [ProtoMember(274)]
            public Inner274 Value274 { get; set; }
            [ProtoMember(275)]
            public Inner275 Value275 { get; set; }
            [ProtoMember(276)]
            public Inner276 Value276 { get; set; }
            [ProtoMember(277)]
            public Inner277 Value277 { get; set; }
            [ProtoMember(278)]
            public Inner278 Value278 { get; set; }
            [ProtoMember(279)]
            public Inner279 Value279 { get; set; }
            [ProtoMember(280)]
            public Inner280 Value280 { get; set; }
            [ProtoMember(281)]
            public Inner281 Value281 { get; set; }
            [ProtoMember(282)]
            public Inner282 Value282 { get; set; }
            [ProtoMember(283)]
            public Inner283 Value283 { get; set; }
            [ProtoMember(284)]
            public Inner284 Value284 { get; set; }
            [ProtoMember(285)]
            public Inner285 Value285 { get; set; }
            [ProtoMember(286)]
            public Inner286 Value286 { get; set; }
            [ProtoMember(287)]
            public Inner287 Value287 { get; set; }
            [ProtoMember(288)]
            public Inner288 Value288 { get; set; }
            [ProtoMember(289)]
            public Inner289 Value289 { get; set; }
            [ProtoMember(290)]
            public Inner290 Value290 { get; set; }
            [ProtoMember(291)]
            public Inner291 Value291 { get; set; }
            [ProtoMember(292)]
            public Inner292 Value292 { get; set; }
            [ProtoMember(293)]
            public Inner293 Value293 { get; set; }
            [ProtoMember(294)]
            public Inner294 Value294 { get; set; }
            [ProtoMember(295)]
            public Inner295 Value295 { get; set; }
            [ProtoMember(296)]
            public Inner296 Value296 { get; set; }
            [ProtoMember(297)]
            public Inner297 Value297 { get; set; }
            [ProtoMember(298)]
            public Inner298 Value298 { get; set; }
            [ProtoMember(299)]
            public Inner299 Value299 { get; set; }
            [ProtoMember(300)]
            public Inner300 Value300 { get; set; }
            [ProtoMember(301)]
            public Inner301 Value301 { get; set; }
            [ProtoMember(302)]
            public Inner302 Value302 { get; set; }
            [ProtoMember(303)]
            public Inner303 Value303 { get; set; }
            [ProtoMember(304)]
            public Inner304 Value304 { get; set; }
            [ProtoMember(305)]
            public Inner305 Value305 { get; set; }
            [ProtoMember(306)]
            public Inner306 Value306 { get; set; }
            [ProtoMember(307)]
            public Inner307 Value307 { get; set; }
            [ProtoMember(308)]
            public Inner308 Value308 { get; set; }
            [ProtoMember(309)]
            public Inner309 Value309 { get; set; }
            [ProtoMember(310)]
            public Inner310 Value310 { get; set; }
            [ProtoMember(311)]
            public Inner311 Value311 { get; set; }
            [ProtoMember(312)]
            public Inner312 Value312 { get; set; }
            [ProtoMember(313)]
            public Inner313 Value313 { get; set; }
            [ProtoMember(314)]
            public Inner314 Value314 { get; set; }
            [ProtoMember(315)]
            public Inner315 Value315 { get; set; }
            [ProtoMember(316)]
            public Inner316 Value316 { get; set; }
            [ProtoMember(317)]
            public Inner317 Value317 { get; set; }
            [ProtoMember(318)]
            public Inner318 Value318 { get; set; }
            [ProtoMember(319)]
            public Inner319 Value319 { get; set; }
            [ProtoMember(320)]
            public Inner320 Value320 { get; set; }
            [ProtoMember(321)]
            public Inner321 Value321 { get; set; }
            [ProtoMember(322)]
            public Inner322 Value322 { get; set; }
            [ProtoMember(323)]
            public Inner323 Value323 { get; set; }
            [ProtoMember(324)]
            public Inner324 Value324 { get; set; }
            [ProtoMember(325)]
            public Inner325 Value325 { get; set; }
            [ProtoMember(326)]
            public Inner326 Value326 { get; set; }
            [ProtoMember(327)]
            public Inner327 Value327 { get; set; }
            [ProtoMember(328)]
            public Inner328 Value328 { get; set; }
            [ProtoMember(329)]
            public Inner329 Value329 { get; set; }
            [ProtoMember(330)]
            public Inner330 Value330 { get; set; }
            [ProtoMember(331)]
            public Inner331 Value331 { get; set; }
            [ProtoMember(332)]
            public Inner332 Value332 { get; set; }
            [ProtoMember(333)]
            public Inner333 Value333 { get; set; }
            [ProtoMember(334)]
            public Inner334 Value334 { get; set; }
            [ProtoMember(335)]
            public Inner335 Value335 { get; set; }
            [ProtoMember(336)]
            public Inner336 Value336 { get; set; }
            [ProtoMember(337)]
            public Inner337 Value337 { get; set; }
            [ProtoMember(338)]
            public Inner338 Value338 { get; set; }
            [ProtoMember(339)]
            public Inner339 Value339 { get; set; }
            [ProtoMember(340)]
            public Inner340 Value340 { get; set; }
            [ProtoMember(341)]
            public Inner341 Value341 { get; set; }
            [ProtoMember(342)]
            public Inner342 Value342 { get; set; }
            [ProtoMember(343)]
            public Inner343 Value343 { get; set; }
            [ProtoMember(344)]
            public Inner344 Value344 { get; set; }
            [ProtoMember(345)]
            public Inner345 Value345 { get; set; }
            [ProtoMember(346)]
            public Inner346 Value346 { get; set; }
            [ProtoMember(347)]
            public Inner347 Value347 { get; set; }
            [ProtoMember(348)]
            public Inner348 Value348 { get; set; }
            [ProtoMember(349)]
            public Inner349 Value349 { get; set; }
            [ProtoMember(350)]
            public Inner350 Value350 { get; set; }
            [ProtoMember(351)]
            public Inner351 Value351 { get; set; }
            [ProtoMember(352)]
            public Inner352 Value352 { get; set; }
            [ProtoMember(353)]
            public Inner353 Value353 { get; set; }
            [ProtoMember(354)]
            public Inner354 Value354 { get; set; }
            [ProtoMember(355)]
            public Inner355 Value355 { get; set; }
            [ProtoMember(356)]
            public Inner356 Value356 { get; set; }
            [ProtoMember(357)]
            public Inner357 Value357 { get; set; }
            [ProtoMember(358)]
            public Inner358 Value358 { get; set; }
            [ProtoMember(359)]
            public Inner359 Value359 { get; set; }
            [ProtoMember(360)]
            public Inner360 Value360 { get; set; }
            [ProtoMember(361)]
            public Inner361 Value361 { get; set; }
            [ProtoMember(362)]
            public Inner362 Value362 { get; set; }
            [ProtoMember(363)]
            public Inner363 Value363 { get; set; }
            [ProtoMember(364)]
            public Inner364 Value364 { get; set; }
            [ProtoMember(365)]
            public Inner365 Value365 { get; set; }
            [ProtoMember(366)]
            public Inner366 Value366 { get; set; }
            [ProtoMember(367)]
            public Inner367 Value367 { get; set; }
            [ProtoMember(368)]
            public Inner368 Value368 { get; set; }
            [ProtoMember(369)]
            public Inner369 Value369 { get; set; }
            [ProtoMember(370)]
            public Inner370 Value370 { get; set; }
            [ProtoMember(371)]
            public Inner371 Value371 { get; set; }
            [ProtoMember(372)]
            public Inner372 Value372 { get; set; }
            [ProtoMember(373)]
            public Inner373 Value373 { get; set; }
            [ProtoMember(374)]
            public Inner374 Value374 { get; set; }
            [ProtoMember(375)]
            public Inner375 Value375 { get; set; }
            [ProtoMember(376)]
            public Inner376 Value376 { get; set; }
            [ProtoMember(377)]
            public Inner377 Value377 { get; set; }
            [ProtoMember(378)]
            public Inner378 Value378 { get; set; }
            [ProtoMember(379)]
            public Inner379 Value379 { get; set; }
            [ProtoMember(380)]
            public Inner380 Value380 { get; set; }
            [ProtoMember(381)]
            public Inner381 Value381 { get; set; }
            [ProtoMember(382)]
            public Inner382 Value382 { get; set; }
            [ProtoMember(383)]
            public Inner383 Value383 { get; set; }
            [ProtoMember(384)]
            public Inner384 Value384 { get; set; }
            [ProtoMember(385)]
            public Inner385 Value385 { get; set; }
            [ProtoMember(386)]
            public Inner386 Value386 { get; set; }
            [ProtoMember(387)]
            public Inner387 Value387 { get; set; }
            [ProtoMember(388)]
            public Inner388 Value388 { get; set; }
            [ProtoMember(389)]
            public Inner389 Value389 { get; set; }
            [ProtoMember(390)]
            public Inner390 Value390 { get; set; }
            [ProtoMember(391)]
            public Inner391 Value391 { get; set; }
            [ProtoMember(392)]
            public Inner392 Value392 { get; set; }
            [ProtoMember(393)]
            public Inner393 Value393 { get; set; }
            [ProtoMember(394)]
            public Inner394 Value394 { get; set; }
            [ProtoMember(395)]
            public Inner395 Value395 { get; set; }
            [ProtoMember(396)]
            public Inner396 Value396 { get; set; }
            [ProtoMember(397)]
            public Inner397 Value397 { get; set; }
            [ProtoMember(398)]
            public Inner398 Value398 { get; set; }
            [ProtoMember(399)]
            public Inner399 Value399 { get; set; }
            [ProtoMember(400)]
            public Inner400 Value400 { get; set; }
            [ProtoMember(401)]
            public Inner401 Value401 { get; set; }
            [ProtoMember(402)]
            public Inner402 Value402 { get; set; }
            [ProtoMember(403)]
            public Inner403 Value403 { get; set; }
            [ProtoMember(404)]
            public Inner404 Value404 { get; set; }
            [ProtoMember(405)]
            public Inner405 Value405 { get; set; }
            [ProtoMember(406)]
            public Inner406 Value406 { get; set; }
            [ProtoMember(407)]
            public Inner407 Value407 { get; set; }
            [ProtoMember(408)]
            public Inner408 Value408 { get; set; }
            [ProtoMember(409)]
            public Inner409 Value409 { get; set; }
            [ProtoMember(410)]
            public Inner410 Value410 { get; set; }
            [ProtoMember(411)]
            public Inner411 Value411 { get; set; }
            [ProtoMember(412)]
            public Inner412 Value412 { get; set; }
            [ProtoMember(413)]
            public Inner413 Value413 { get; set; }
            [ProtoMember(414)]
            public Inner414 Value414 { get; set; }
            [ProtoMember(415)]
            public Inner415 Value415 { get; set; }
            [ProtoMember(416)]
            public Inner416 Value416 { get; set; }
            [ProtoMember(417)]
            public Inner417 Value417 { get; set; }
            [ProtoMember(418)]
            public Inner418 Value418 { get; set; }
            [ProtoMember(419)]
            public Inner419 Value419 { get; set; }
            [ProtoMember(420)]
            public Inner420 Value420 { get; set; }
            [ProtoMember(421)]
            public Inner421 Value421 { get; set; }
            [ProtoMember(422)]
            public Inner422 Value422 { get; set; }
            [ProtoMember(423)]
            public Inner423 Value423 { get; set; }
            [ProtoMember(424)]
            public Inner424 Value424 { get; set; }
            [ProtoMember(425)]
            public Inner425 Value425 { get; set; }
            [ProtoMember(426)]
            public Inner426 Value426 { get; set; }
            [ProtoMember(427)]
            public Inner427 Value427 { get; set; }
            [ProtoMember(428)]
            public Inner428 Value428 { get; set; }
            [ProtoMember(429)]
            public Inner429 Value429 { get; set; }
            [ProtoMember(430)]
            public Inner430 Value430 { get; set; }
            [ProtoMember(431)]
            public Inner431 Value431 { get; set; }
            [ProtoMember(432)]
            public Inner432 Value432 { get; set; }
            [ProtoMember(433)]
            public Inner433 Value433 { get; set; }
            [ProtoMember(434)]
            public Inner434 Value434 { get; set; }
            [ProtoMember(435)]
            public Inner435 Value435 { get; set; }
            [ProtoMember(436)]
            public Inner436 Value436 { get; set; }
            [ProtoMember(437)]
            public Inner437 Value437 { get; set; }
            [ProtoMember(438)]
            public Inner438 Value438 { get; set; }
            [ProtoMember(439)]
            public Inner439 Value439 { get; set; }
            [ProtoMember(440)]
            public Inner440 Value440 { get; set; }
            [ProtoMember(441)]
            public Inner441 Value441 { get; set; }
            [ProtoMember(442)]
            public Inner442 Value442 { get; set; }
            [ProtoMember(443)]
            public Inner443 Value443 { get; set; }
            [ProtoMember(444)]
            public Inner444 Value444 { get; set; }
            [ProtoMember(445)]
            public Inner445 Value445 { get; set; }
            [ProtoMember(446)]
            public Inner446 Value446 { get; set; }
            [ProtoMember(447)]
            public Inner447 Value447 { get; set; }
            [ProtoMember(448)]
            public Inner448 Value448 { get; set; }
            [ProtoMember(449)]
            public Inner449 Value449 { get; set; }
            [ProtoMember(450)]
            public Inner450 Value450 { get; set; }
            [ProtoMember(451)]
            public Inner451 Value451 { get; set; }
            [ProtoMember(452)]
            public Inner452 Value452 { get; set; }
            [ProtoMember(453)]
            public Inner453 Value453 { get; set; }
            [ProtoMember(454)]
            public Inner454 Value454 { get; set; }
            [ProtoMember(455)]
            public Inner455 Value455 { get; set; }
            [ProtoMember(456)]
            public Inner456 Value456 { get; set; }
            [ProtoMember(457)]
            public Inner457 Value457 { get; set; }
            [ProtoMember(458)]
            public Inner458 Value458 { get; set; }
            [ProtoMember(459)]
            public Inner459 Value459 { get; set; }
            [ProtoMember(460)]
            public Inner460 Value460 { get; set; }
            [ProtoMember(461)]
            public Inner461 Value461 { get; set; }
            [ProtoMember(462)]
            public Inner462 Value462 { get; set; }
            [ProtoMember(463)]
            public Inner463 Value463 { get; set; }
            [ProtoMember(464)]
            public Inner464 Value464 { get; set; }
            [ProtoMember(465)]
            public Inner465 Value465 { get; set; }
            [ProtoMember(466)]
            public Inner466 Value466 { get; set; }
            [ProtoMember(467)]
            public Inner467 Value467 { get; set; }
            [ProtoMember(468)]
            public Inner468 Value468 { get; set; }
            [ProtoMember(469)]
            public Inner469 Value469 { get; set; }
            [ProtoMember(470)]
            public Inner470 Value470 { get; set; }
            [ProtoMember(471)]
            public Inner471 Value471 { get; set; }
            [ProtoMember(472)]
            public Inner472 Value472 { get; set; }
            [ProtoMember(473)]
            public Inner473 Value473 { get; set; }
            [ProtoMember(474)]
            public Inner474 Value474 { get; set; }
            [ProtoMember(475)]
            public Inner475 Value475 { get; set; }
            [ProtoMember(476)]
            public Inner476 Value476 { get; set; }
            [ProtoMember(477)]
            public Inner477 Value477 { get; set; }
            [ProtoMember(478)]
            public Inner478 Value478 { get; set; }
            [ProtoMember(479)]
            public Inner479 Value479 { get; set; }
            [ProtoMember(480)]
            public Inner480 Value480 { get; set; }
            [ProtoMember(481)]
            public Inner481 Value481 { get; set; }
            [ProtoMember(482)]
            public Inner482 Value482 { get; set; }
            [ProtoMember(483)]
            public Inner483 Value483 { get; set; }
            [ProtoMember(484)]
            public Inner484 Value484 { get; set; }
            [ProtoMember(485)]
            public Inner485 Value485 { get; set; }
            [ProtoMember(486)]
            public Inner486 Value486 { get; set; }
            [ProtoMember(487)]
            public Inner487 Value487 { get; set; }
            [ProtoMember(488)]
            public Inner488 Value488 { get; set; }
            [ProtoMember(489)]
            public Inner489 Value489 { get; set; }
            [ProtoMember(490)]
            public Inner490 Value490 { get; set; }
            [ProtoMember(491)]
            public Inner491 Value491 { get; set; }
            [ProtoMember(492)]
            public Inner492 Value492 { get; set; }
            [ProtoMember(493)]
            public Inner493 Value493 { get; set; }
            [ProtoMember(494)]
            public Inner494 Value494 { get; set; }
            [ProtoMember(495)]
            public Inner495 Value495 { get; set; }
            [ProtoMember(496)]
            public Inner496 Value496 { get; set; }
            [ProtoMember(497)]
            public Inner497 Value497 { get; set; }
            [ProtoMember(498)]
            public Inner498 Value498 { get; set; }
            [ProtoMember(499)]
            public Inner499 Value499 { get; set; }
            [ProtoMember(500)]
            public Inner500 Value500 { get; set; }
            [ProtoMember(501)]
            public Inner501 Value501 { get; set; }
            [ProtoMember(502)]
            public Inner502 Value502 { get; set; }
            [ProtoMember(503)]
            public Inner503 Value503 { get; set; }
            [ProtoMember(504)]
            public Inner504 Value504 { get; set; }
            [ProtoMember(505)]
            public Inner505 Value505 { get; set; }
            [ProtoMember(506)]
            public Inner506 Value506 { get; set; }
            [ProtoMember(507)]
            public Inner507 Value507 { get; set; }
            [ProtoMember(508)]
            public Inner508 Value508 { get; set; }
            [ProtoMember(509)]
            public Inner509 Value509 { get; set; }
            [ProtoMember(510)]
            public Inner510 Value510 { get; set; }
            [ProtoMember(511)]
            public Inner511 Value511 { get; set; }
            [ProtoMember(512)]
            public Inner512 Value512 { get; set; }
            [ProtoMember(513)]
            public Inner513 Value513 { get; set; }
            [ProtoMember(514)]
            public Inner514 Value514 { get; set; }
            [ProtoMember(515)]
            public Inner515 Value515 { get; set; }
            [ProtoMember(516)]
            public Inner516 Value516 { get; set; }
            [ProtoMember(517)]
            public Inner517 Value517 { get; set; }
            [ProtoMember(518)]
            public Inner518 Value518 { get; set; }
            [ProtoMember(519)]
            public Inner519 Value519 { get; set; }
            [ProtoMember(520)]
            public Inner520 Value520 { get; set; }
            [ProtoMember(521)]
            public Inner521 Value521 { get; set; }
            [ProtoMember(522)]
            public Inner522 Value522 { get; set; }
            [ProtoMember(523)]
            public Inner523 Value523 { get; set; }
            [ProtoMember(524)]
            public Inner524 Value524 { get; set; }
            [ProtoMember(525)]
            public Inner525 Value525 { get; set; }
            [ProtoMember(526)]
            public Inner526 Value526 { get; set; }
            [ProtoMember(527)]
            public Inner527 Value527 { get; set; }
            [ProtoMember(528)]
            public Inner528 Value528 { get; set; }
            [ProtoMember(529)]
            public Inner529 Value529 { get; set; }
            [ProtoMember(530)]
            public Inner530 Value530 { get; set; }
            [ProtoMember(531)]
            public Inner531 Value531 { get; set; }
            [ProtoMember(532)]
            public Inner532 Value532 { get; set; }
            [ProtoMember(533)]
            public Inner533 Value533 { get; set; }
            [ProtoMember(534)]
            public Inner534 Value534 { get; set; }
            [ProtoMember(535)]
            public Inner535 Value535 { get; set; }
            [ProtoMember(536)]
            public Inner536 Value536 { get; set; }
            [ProtoMember(537)]
            public Inner537 Value537 { get; set; }
            [ProtoMember(538)]
            public Inner538 Value538 { get; set; }
            [ProtoMember(539)]
            public Inner539 Value539 { get; set; }
            [ProtoMember(540)]
            public Inner540 Value540 { get; set; }
            [ProtoMember(541)]
            public Inner541 Value541 { get; set; }
            [ProtoMember(542)]
            public Inner542 Value542 { get; set; }
            [ProtoMember(543)]
            public Inner543 Value543 { get; set; }
            [ProtoMember(544)]
            public Inner544 Value544 { get; set; }
            [ProtoMember(545)]
            public Inner545 Value545 { get; set; }
            [ProtoMember(546)]
            public Inner546 Value546 { get; set; }
            [ProtoMember(547)]
            public Inner547 Value547 { get; set; }
            [ProtoMember(548)]
            public Inner548 Value548 { get; set; }
            [ProtoMember(549)]
            public Inner549 Value549 { get; set; }
            [ProtoMember(550)]
            public Inner550 Value550 { get; set; }
            [ProtoMember(551)]
            public Inner551 Value551 { get; set; }
            [ProtoMember(552)]
            public Inner552 Value552 { get; set; }
            [ProtoMember(553)]
            public Inner553 Value553 { get; set; }
            [ProtoMember(554)]
            public Inner554 Value554 { get; set; }
            [ProtoMember(555)]
            public Inner555 Value555 { get; set; }
            [ProtoMember(556)]
            public Inner556 Value556 { get; set; }
            [ProtoMember(557)]
            public Inner557 Value557 { get; set; }
            [ProtoMember(558)]
            public Inner558 Value558 { get; set; }
            [ProtoMember(559)]
            public Inner559 Value559 { get; set; }
            [ProtoMember(560)]
            public Inner560 Value560 { get; set; }
            [ProtoMember(561)]
            public Inner561 Value561 { get; set; }
            [ProtoMember(562)]
            public Inner562 Value562 { get; set; }
            [ProtoMember(563)]
            public Inner563 Value563 { get; set; }
            [ProtoMember(564)]
            public Inner564 Value564 { get; set; }
            [ProtoMember(565)]
            public Inner565 Value565 { get; set; }
            [ProtoMember(566)]
            public Inner566 Value566 { get; set; }
            [ProtoMember(567)]
            public Inner567 Value567 { get; set; }
            [ProtoMember(568)]
            public Inner568 Value568 { get; set; }
            [ProtoMember(569)]
            public Inner569 Value569 { get; set; }
            [ProtoMember(570)]
            public Inner570 Value570 { get; set; }
            [ProtoMember(571)]
            public Inner571 Value571 { get; set; }
            [ProtoMember(572)]
            public Inner572 Value572 { get; set; }
            [ProtoMember(573)]
            public Inner573 Value573 { get; set; }
            [ProtoMember(574)]
            public Inner574 Value574 { get; set; }
            [ProtoMember(575)]
            public Inner575 Value575 { get; set; }
            [ProtoMember(576)]
            public Inner576 Value576 { get; set; }
            [ProtoMember(577)]
            public Inner577 Value577 { get; set; }
            [ProtoMember(578)]
            public Inner578 Value578 { get; set; }
            [ProtoMember(579)]
            public Inner579 Value579 { get; set; }
            [ProtoMember(580)]
            public Inner580 Value580 { get; set; }
            [ProtoMember(581)]
            public Inner581 Value581 { get; set; }
            [ProtoMember(582)]
            public Inner582 Value582 { get; set; }
            [ProtoMember(583)]
            public Inner583 Value583 { get; set; }
            [ProtoMember(584)]
            public Inner584 Value584 { get; set; }
            [ProtoMember(585)]
            public Inner585 Value585 { get; set; }
            [ProtoMember(586)]
            public Inner586 Value586 { get; set; }
            [ProtoMember(587)]
            public Inner587 Value587 { get; set; }
            [ProtoMember(588)]
            public Inner588 Value588 { get; set; }
            [ProtoMember(589)]
            public Inner589 Value589 { get; set; }
            [ProtoMember(590)]
            public Inner590 Value590 { get; set; }
            [ProtoMember(591)]
            public Inner591 Value591 { get; set; }
            [ProtoMember(592)]
            public Inner592 Value592 { get; set; }
            [ProtoMember(593)]
            public Inner593 Value593 { get; set; }
            [ProtoMember(594)]
            public Inner594 Value594 { get; set; }
            [ProtoMember(595)]
            public Inner595 Value595 { get; set; }
            [ProtoMember(596)]
            public Inner596 Value596 { get; set; }
            [ProtoMember(597)]
            public Inner597 Value597 { get; set; }
            [ProtoMember(598)]
            public Inner598 Value598 { get; set; }
            [ProtoMember(599)]
            public Inner599 Value599 { get; set; }
            [ProtoMember(600)]
            public Inner600 Value600 { get; set; }
            [ProtoMember(601)]
            public Inner601 Value601 { get; set; }
            [ProtoMember(602)]
            public Inner602 Value602 { get; set; }
            [ProtoMember(603)]
            public Inner603 Value603 { get; set; }
            [ProtoMember(604)]
            public Inner604 Value604 { get; set; }
            [ProtoMember(605)]
            public Inner605 Value605 { get; set; }
            [ProtoMember(606)]
            public Inner606 Value606 { get; set; }
            [ProtoMember(607)]
            public Inner607 Value607 { get; set; }
            [ProtoMember(608)]
            public Inner608 Value608 { get; set; }
            [ProtoMember(609)]
            public Inner609 Value609 { get; set; }
            [ProtoMember(610)]
            public Inner610 Value610 { get; set; }
            [ProtoMember(611)]
            public Inner611 Value611 { get; set; }
            [ProtoMember(612)]
            public Inner612 Value612 { get; set; }
            [ProtoMember(613)]
            public Inner613 Value613 { get; set; }
            [ProtoMember(614)]
            public Inner614 Value614 { get; set; }
            [ProtoMember(615)]
            public Inner615 Value615 { get; set; }
            [ProtoMember(616)]
            public Inner616 Value616 { get; set; }
            [ProtoMember(617)]
            public Inner617 Value617 { get; set; }
            [ProtoMember(618)]
            public Inner618 Value618 { get; set; }
            [ProtoMember(619)]
            public Inner619 Value619 { get; set; }
            [ProtoMember(620)]
            public Inner620 Value620 { get; set; }
            [ProtoMember(621)]
            public Inner621 Value621 { get; set; }
            [ProtoMember(622)]
            public Inner622 Value622 { get; set; }
            [ProtoMember(623)]
            public Inner623 Value623 { get; set; }
            [ProtoMember(624)]
            public Inner624 Value624 { get; set; }
            [ProtoMember(625)]
            public Inner625 Value625 { get; set; }
            [ProtoMember(626)]
            public Inner626 Value626 { get; set; }
            [ProtoMember(627)]
            public Inner627 Value627 { get; set; }
            [ProtoMember(628)]
            public Inner628 Value628 { get; set; }
            [ProtoMember(629)]
            public Inner629 Value629 { get; set; }
            [ProtoMember(630)]
            public Inner630 Value630 { get; set; }
            [ProtoMember(631)]
            public Inner631 Value631 { get; set; }
            [ProtoMember(632)]
            public Inner632 Value632 { get; set; }
            [ProtoMember(633)]
            public Inner633 Value633 { get; set; }
            [ProtoMember(634)]
            public Inner634 Value634 { get; set; }
            [ProtoMember(635)]
            public Inner635 Value635 { get; set; }
            [ProtoMember(636)]
            public Inner636 Value636 { get; set; }
            [ProtoMember(637)]
            public Inner637 Value637 { get; set; }
            [ProtoMember(638)]
            public Inner638 Value638 { get; set; }
            [ProtoMember(639)]
            public Inner639 Value639 { get; set; }
            [ProtoMember(640)]
            public Inner640 Value640 { get; set; }
            [ProtoMember(641)]
            public Inner641 Value641 { get; set; }
            [ProtoMember(642)]
            public Inner642 Value642 { get; set; }
            [ProtoMember(643)]
            public Inner643 Value643 { get; set; }
            [ProtoMember(644)]
            public Inner644 Value644 { get; set; }
            [ProtoMember(645)]
            public Inner645 Value645 { get; set; }
            [ProtoMember(646)]
            public Inner646 Value646 { get; set; }
            [ProtoMember(647)]
            public Inner647 Value647 { get; set; }
            [ProtoMember(648)]
            public Inner648 Value648 { get; set; }
            [ProtoMember(649)]
            public Inner649 Value649 { get; set; }
            [ProtoMember(650)]
            public Inner650 Value650 { get; set; }
            [ProtoMember(651)]
            public Inner651 Value651 { get; set; }
            [ProtoMember(652)]
            public Inner652 Value652 { get; set; }
            [ProtoMember(653)]
            public Inner653 Value653 { get; set; }
            [ProtoMember(654)]
            public Inner654 Value654 { get; set; }
            [ProtoMember(655)]
            public Inner655 Value655 { get; set; }
            [ProtoMember(656)]
            public Inner656 Value656 { get; set; }
            [ProtoMember(657)]
            public Inner657 Value657 { get; set; }
            [ProtoMember(658)]
            public Inner658 Value658 { get; set; }
            [ProtoMember(659)]
            public Inner659 Value659 { get; set; }
            [ProtoMember(660)]
            public Inner660 Value660 { get; set; }
            [ProtoMember(661)]
            public Inner661 Value661 { get; set; }
            [ProtoMember(662)]
            public Inner662 Value662 { get; set; }
            [ProtoMember(663)]
            public Inner663 Value663 { get; set; }
            [ProtoMember(664)]
            public Inner664 Value664 { get; set; }
            [ProtoMember(665)]
            public Inner665 Value665 { get; set; }
            [ProtoMember(666)]
            public Inner666 Value666 { get; set; }
            [ProtoMember(667)]
            public Inner667 Value667 { get; set; }
            [ProtoMember(668)]
            public Inner668 Value668 { get; set; }
            [ProtoMember(669)]
            public Inner669 Value669 { get; set; }
            [ProtoMember(670)]
            public Inner670 Value670 { get; set; }
            [ProtoMember(671)]
            public Inner671 Value671 { get; set; }
            [ProtoMember(672)]
            public Inner672 Value672 { get; set; }
            [ProtoMember(673)]
            public Inner673 Value673 { get; set; }
            [ProtoMember(674)]
            public Inner674 Value674 { get; set; }
            [ProtoMember(675)]
            public Inner675 Value675 { get; set; }
            [ProtoMember(676)]
            public Inner676 Value676 { get; set; }
            [ProtoMember(677)]
            public Inner677 Value677 { get; set; }
            [ProtoMember(678)]
            public Inner678 Value678 { get; set; }
            [ProtoMember(679)]
            public Inner679 Value679 { get; set; }
            [ProtoMember(680)]
            public Inner680 Value680 { get; set; }
            [ProtoMember(681)]
            public Inner681 Value681 { get; set; }
            [ProtoMember(682)]
            public Inner682 Value682 { get; set; }
            [ProtoMember(683)]
            public Inner683 Value683 { get; set; }
            [ProtoMember(684)]
            public Inner684 Value684 { get; set; }
            [ProtoMember(685)]
            public Inner685 Value685 { get; set; }
            [ProtoMember(686)]
            public Inner686 Value686 { get; set; }
            [ProtoMember(687)]
            public Inner687 Value687 { get; set; }
            [ProtoMember(688)]
            public Inner688 Value688 { get; set; }
            [ProtoMember(689)]
            public Inner689 Value689 { get; set; }
            [ProtoMember(690)]
            public Inner690 Value690 { get; set; }
            [ProtoMember(691)]
            public Inner691 Value691 { get; set; }
            [ProtoMember(692)]
            public Inner692 Value692 { get; set; }
            [ProtoMember(693)]
            public Inner693 Value693 { get; set; }
            [ProtoMember(694)]
            public Inner694 Value694 { get; set; }
            [ProtoMember(695)]
            public Inner695 Value695 { get; set; }
            [ProtoMember(696)]
            public Inner696 Value696 { get; set; }
            [ProtoMember(697)]
            public Inner697 Value697 { get; set; }
            [ProtoMember(698)]
            public Inner698 Value698 { get; set; }
            [ProtoMember(699)]
            public Inner699 Value699 { get; set; }
            [ProtoMember(700)]
            public Inner700 Value700 { get; set; }
            [ProtoMember(701)]
            public Inner701 Value701 { get; set; }
            [ProtoMember(702)]
            public Inner702 Value702 { get; set; }
            [ProtoMember(703)]
            public Inner703 Value703 { get; set; }
            [ProtoMember(704)]
            public Inner704 Value704 { get; set; }
            [ProtoMember(705)]
            public Inner705 Value705 { get; set; }
            [ProtoMember(706)]
            public Inner706 Value706 { get; set; }
            [ProtoMember(707)]
            public Inner707 Value707 { get; set; }
            [ProtoMember(708)]
            public Inner708 Value708 { get; set; }
            [ProtoMember(709)]
            public Inner709 Value709 { get; set; }
            [ProtoMember(710)]
            public Inner710 Value710 { get; set; }
            [ProtoMember(711)]
            public Inner711 Value711 { get; set; }
            [ProtoMember(712)]
            public Inner712 Value712 { get; set; }
            [ProtoMember(713)]
            public Inner713 Value713 { get; set; }
            [ProtoMember(714)]
            public Inner714 Value714 { get; set; }
            [ProtoMember(715)]
            public Inner715 Value715 { get; set; }
            [ProtoMember(716)]
            public Inner716 Value716 { get; set; }
            [ProtoMember(717)]
            public Inner717 Value717 { get; set; }
            [ProtoMember(718)]
            public Inner718 Value718 { get; set; }
            [ProtoMember(719)]
            public Inner719 Value719 { get; set; }
            [ProtoMember(720)]
            public Inner720 Value720 { get; set; }
            [ProtoMember(721)]
            public Inner721 Value721 { get; set; }
            [ProtoMember(722)]
            public Inner722 Value722 { get; set; }
            [ProtoMember(723)]
            public Inner723 Value723 { get; set; }
            [ProtoMember(724)]
            public Inner724 Value724 { get; set; }
            [ProtoMember(725)]
            public Inner725 Value725 { get; set; }
            [ProtoMember(726)]
            public Inner726 Value726 { get; set; }
            [ProtoMember(727)]
            public Inner727 Value727 { get; set; }
            [ProtoMember(728)]
            public Inner728 Value728 { get; set; }
            [ProtoMember(729)]
            public Inner729 Value729 { get; set; }
            [ProtoMember(730)]
            public Inner730 Value730 { get; set; }
            [ProtoMember(731)]
            public Inner731 Value731 { get; set; }
            [ProtoMember(732)]
            public Inner732 Value732 { get; set; }
            [ProtoMember(733)]
            public Inner733 Value733 { get; set; }
            [ProtoMember(734)]
            public Inner734 Value734 { get; set; }
            [ProtoMember(735)]
            public Inner735 Value735 { get; set; }
            [ProtoMember(736)]
            public Inner736 Value736 { get; set; }
            [ProtoMember(737)]
            public Inner737 Value737 { get; set; }
            [ProtoMember(738)]
            public Inner738 Value738 { get; set; }
            [ProtoMember(739)]
            public Inner739 Value739 { get; set; }
            [ProtoMember(740)]
            public Inner740 Value740 { get; set; }
            [ProtoMember(741)]
            public Inner741 Value741 { get; set; }
            [ProtoMember(742)]
            public Inner742 Value742 { get; set; }
            [ProtoMember(743)]
            public Inner743 Value743 { get; set; }
            [ProtoMember(744)]
            public Inner744 Value744 { get; set; }
            [ProtoMember(745)]
            public Inner745 Value745 { get; set; }
            [ProtoMember(746)]
            public Inner746 Value746 { get; set; }
            [ProtoMember(747)]
            public Inner747 Value747 { get; set; }
            [ProtoMember(748)]
            public Inner748 Value748 { get; set; }
            [ProtoMember(749)]
            public Inner749 Value749 { get; set; }
            [ProtoMember(750)]
            public Inner750 Value750 { get; set; }
            [ProtoMember(751)]
            public Inner751 Value751 { get; set; }
            [ProtoMember(752)]
            public Inner752 Value752 { get; set; }
            [ProtoMember(753)]
            public Inner753 Value753 { get; set; }
            [ProtoMember(754)]
            public Inner754 Value754 { get; set; }
            [ProtoMember(755)]
            public Inner755 Value755 { get; set; }
            [ProtoMember(756)]
            public Inner756 Value756 { get; set; }
            [ProtoMember(757)]
            public Inner757 Value757 { get; set; }
            [ProtoMember(758)]
            public Inner758 Value758 { get; set; }
            [ProtoMember(759)]
            public Inner759 Value759 { get; set; }
            [ProtoMember(760)]
            public Inner760 Value760 { get; set; }
            [ProtoMember(761)]
            public Inner761 Value761 { get; set; }
            [ProtoMember(762)]
            public Inner762 Value762 { get; set; }
            [ProtoMember(763)]
            public Inner763 Value763 { get; set; }
            [ProtoMember(764)]
            public Inner764 Value764 { get; set; }
            [ProtoMember(765)]
            public Inner765 Value765 { get; set; }
            [ProtoMember(766)]
            public Inner766 Value766 { get; set; }
            [ProtoMember(767)]
            public Inner767 Value767 { get; set; }
            [ProtoMember(768)]
            public Inner768 Value768 { get; set; }
            [ProtoMember(769)]
            public Inner769 Value769 { get; set; }
            [ProtoMember(770)]
            public Inner770 Value770 { get; set; }
            [ProtoMember(771)]
            public Inner771 Value771 { get; set; }
            [ProtoMember(772)]
            public Inner772 Value772 { get; set; }
            [ProtoMember(773)]
            public Inner773 Value773 { get; set; }
            [ProtoMember(774)]
            public Inner774 Value774 { get; set; }
            [ProtoMember(775)]
            public Inner775 Value775 { get; set; }
            [ProtoMember(776)]
            public Inner776 Value776 { get; set; }
            [ProtoMember(777)]
            public Inner777 Value777 { get; set; }
            [ProtoMember(778)]
            public Inner778 Value778 { get; set; }
            [ProtoMember(779)]
            public Inner779 Value779 { get; set; }
            [ProtoMember(780)]
            public Inner780 Value780 { get; set; }
            [ProtoMember(781)]
            public Inner781 Value781 { get; set; }
            [ProtoMember(782)]
            public Inner782 Value782 { get; set; }
            [ProtoMember(783)]
            public Inner783 Value783 { get; set; }
            [ProtoMember(784)]
            public Inner784 Value784 { get; set; }
            [ProtoMember(785)]
            public Inner785 Value785 { get; set; }
            [ProtoMember(786)]
            public Inner786 Value786 { get; set; }
            [ProtoMember(787)]
            public Inner787 Value787 { get; set; }
            [ProtoMember(788)]
            public Inner788 Value788 { get; set; }
            [ProtoMember(789)]
            public Inner789 Value789 { get; set; }
            [ProtoMember(790)]
            public Inner790 Value790 { get; set; }
            [ProtoMember(791)]
            public Inner791 Value791 { get; set; }
            [ProtoMember(792)]
            public Inner792 Value792 { get; set; }
            [ProtoMember(793)]
            public Inner793 Value793 { get; set; }
            [ProtoMember(794)]
            public Inner794 Value794 { get; set; }
            [ProtoMember(795)]
            public Inner795 Value795 { get; set; }
            [ProtoMember(796)]
            public Inner796 Value796 { get; set; }
            [ProtoMember(797)]
            public Inner797 Value797 { get; set; }
            [ProtoMember(798)]
            public Inner798 Value798 { get; set; }
            [ProtoMember(799)]
            public Inner799 Value799 { get; set; }
            [ProtoMember(800)]
            public Inner800 Value800 { get; set; }
        }
        [ProtoContract]
        public class Inner1 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner2 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner3 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner4 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner5 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner6 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner7 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner8 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner9 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner10 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner11 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner12 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner13 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner14 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner15 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner16 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner17 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner18 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner19 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner20 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner21 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner22 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner23 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner24 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner25 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner26 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner27 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner28 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner29 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner30 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner31 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner32 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner33 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner34 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner35 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner36 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner37 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner38 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner39 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner40 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner41 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner42 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner43 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner44 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner45 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner46 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner47 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner48 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner49 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner50 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner51 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner52 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner53 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner54 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner55 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner56 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner57 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner58 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner59 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner60 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner61 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner62 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner63 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner64 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner65 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner66 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner67 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner68 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner69 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner70 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner71 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner72 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner73 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner74 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner75 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner76 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner77 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner78 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner79 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner80 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner81 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner82 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner83 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner84 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner85 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner86 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner87 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner88 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner89 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner90 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner91 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner92 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner93 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner94 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner95 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner96 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner97 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner98 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner99 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner100 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner101 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner102 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner103 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner104 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner105 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner106 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner107 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner108 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner109 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner110 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner111 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner112 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner113 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner114 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner115 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner116 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner117 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner118 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner119 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner120 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner121 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner122 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner123 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner124 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner125 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner126 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner127 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner128 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner129 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner130 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner131 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner132 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner133 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner134 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner135 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner136 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner137 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner138 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner139 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner140 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner141 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner142 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner143 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner144 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner145 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner146 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner147 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner148 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner149 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner150 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner151 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner152 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner153 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner154 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner155 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner156 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner157 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner158 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner159 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner160 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner161 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner162 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner163 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner164 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner165 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner166 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner167 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner168 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner169 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner170 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner171 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner172 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner173 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner174 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner175 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner176 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner177 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner178 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner179 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner180 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner181 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner182 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner183 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner184 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner185 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner186 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner187 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner188 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner189 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner190 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner191 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner192 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner193 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner194 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner195 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner196 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner197 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner198 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner199 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner200 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner201 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner202 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner203 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner204 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner205 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner206 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner207 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner208 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner209 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner210 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner211 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner212 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner213 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner214 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner215 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner216 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner217 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner218 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner219 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner220 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner221 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner222 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner223 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner224 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner225 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner226 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner227 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner228 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner229 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner230 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner231 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner232 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner233 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner234 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner235 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner236 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner237 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner238 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner239 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner240 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner241 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner242 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner243 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner244 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner245 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner246 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner247 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner248 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner249 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner250 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner251 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner252 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner253 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner254 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner255 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner256 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner257 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner258 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner259 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner260 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner261 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner262 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner263 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner264 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner265 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner266 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner267 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner268 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner269 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner270 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner271 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner272 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner273 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner274 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner275 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner276 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner277 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner278 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner279 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner280 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner281 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner282 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner283 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner284 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner285 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner286 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner287 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner288 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner289 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner290 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner291 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner292 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner293 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner294 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner295 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner296 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner297 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner298 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner299 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner300 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner301 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner302 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner303 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner304 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner305 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner306 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner307 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner308 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner309 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner310 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner311 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner312 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner313 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner314 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner315 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner316 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner317 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner318 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner319 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner320 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner321 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner322 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner323 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner324 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner325 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner326 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner327 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner328 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner329 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner330 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner331 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner332 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner333 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner334 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner335 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner336 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner337 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner338 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner339 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner340 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner341 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner342 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner343 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner344 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner345 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner346 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner347 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner348 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner349 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner350 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner351 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner352 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner353 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner354 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner355 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner356 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner357 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner358 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner359 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner360 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner361 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner362 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner363 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner364 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner365 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner366 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner367 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner368 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner369 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner370 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner371 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner372 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner373 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner374 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner375 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner376 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner377 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner378 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner379 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner380 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner381 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner382 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner383 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner384 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner385 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner386 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner387 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner388 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner389 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner390 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner391 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner392 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner393 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner394 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner395 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner396 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner397 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner398 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner399 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner400 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner401 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner402 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner403 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner404 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner405 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner406 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner407 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner408 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner409 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner410 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner411 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner412 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner413 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner414 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner415 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner416 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner417 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner418 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner419 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner420 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner421 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner422 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner423 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner424 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner425 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner426 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner427 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner428 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner429 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner430 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner431 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner432 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner433 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner434 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner435 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner436 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner437 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner438 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner439 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner440 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner441 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner442 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner443 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner444 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner445 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner446 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner447 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner448 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner449 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner450 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner451 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner452 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner453 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner454 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner455 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner456 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner457 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner458 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner459 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner460 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner461 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner462 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner463 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner464 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner465 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner466 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner467 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner468 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner469 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner470 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner471 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner472 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner473 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner474 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner475 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner476 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner477 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner478 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner479 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner480 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner481 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner482 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner483 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner484 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner485 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner486 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner487 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner488 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner489 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner490 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner491 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner492 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner493 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner494 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner495 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner496 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner497 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner498 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner499 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner500 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner501 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner502 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner503 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner504 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner505 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner506 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner507 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner508 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner509 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner510 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner511 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner512 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner513 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner514 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner515 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner516 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner517 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner518 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner519 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner520 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner521 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner522 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner523 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner524 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner525 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner526 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner527 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner528 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner529 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner530 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner531 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner532 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner533 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner534 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner535 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner536 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner537 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner538 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner539 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner540 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner541 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner542 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner543 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner544 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner545 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner546 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner547 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner548 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner549 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner550 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner551 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner552 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner553 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner554 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner555 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner556 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner557 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner558 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner559 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner560 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner561 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner562 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner563 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner564 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner565 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner566 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner567 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner568 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner569 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner570 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner571 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner572 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner573 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner574 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner575 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner576 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner577 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner578 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner579 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner580 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner581 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner582 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner583 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner584 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner585 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner586 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner587 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner588 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner589 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner590 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner591 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner592 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner593 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner594 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner595 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner596 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner597 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner598 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner599 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner600 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner601 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner602 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner603 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner604 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner605 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner606 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner607 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner608 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner609 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner610 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner611 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner612 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner613 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner614 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner615 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner616 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner617 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner618 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner619 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner620 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner621 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner622 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner623 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner624 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner625 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner626 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner627 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner628 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner629 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner630 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner631 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner632 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner633 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner634 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner635 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner636 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner637 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner638 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner639 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner640 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner641 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner642 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner643 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner644 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner645 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner646 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner647 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner648 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner649 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner650 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner651 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner652 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner653 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner654 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner655 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner656 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner657 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner658 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner659 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner660 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner661 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner662 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner663 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner664 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner665 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner666 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner667 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner668 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner669 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner670 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner671 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner672 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner673 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner674 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner675 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner676 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner677 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner678 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner679 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner680 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner681 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner682 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner683 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner684 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner685 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner686 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner687 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner688 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner689 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner690 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner691 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner692 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner693 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner694 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner695 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner696 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner697 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner698 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner699 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner700 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner701 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner702 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner703 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner704 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner705 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner706 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner707 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner708 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner709 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner710 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner711 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner712 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner713 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner714 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner715 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner716 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner717 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner718 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner719 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner720 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner721 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner722 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner723 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner724 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner725 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner726 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner727 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner728 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner729 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner730 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner731 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner732 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner733 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner734 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner735 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner736 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner737 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner738 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner739 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner740 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner741 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner742 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner743 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner744 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner745 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner746 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner747 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner748 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner749 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner750 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner751 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner752 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner753 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner754 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner755 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner756 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner757 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner758 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner759 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner760 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner761 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner762 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner763 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner764 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner765 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner766 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner767 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner768 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner769 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner770 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner771 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner772 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner773 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner774 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner775 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner776 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner777 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner778 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner779 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner780 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner781 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner782 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner783 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner784 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner785 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner786 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner787 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner788 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner789 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner790 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner791 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner792 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner793 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner794 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner795 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner796 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner797 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner798 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner799 { [ProtoMember(1)] public int Value { get; set; } }
        [ProtoContract]
        public class Inner800 { [ProtoMember(1)] public int Value { get; set; } }

    }
}
