using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using NUnit.Framework;

namespace Examples
{
    [TestFixture]
    public class TestAutoFields
    {

        [ProtoContract]
        [ProtoPartialMember(2, "IncludeIndirect")]
        [ProtoPartialIgnore("IgnoreIndirect")]
        public class IgnorePOCO
        {
            [ProtoMember(1)]
            public int IncludeDirect { get; set; }

            public int IncludeIndirect { get; set; }

            [ProtoMember(3)]
            [ProtoIgnore]
            public int IgnoreDirect { get; set; }

            [ProtoMember(4)]
            public int IgnoreIndirect { get; set; }
        }

        [Test]
        public void TestIgnore()
        {
            IgnorePOCO foo = new IgnorePOCO
                             {
                                 IgnoreDirect = 1,
                                 IgnoreIndirect = 2,
                                 IncludeDirect = 3,
                                 IncludeIndirect = 4
                             },
                       bar = Serializer.DeepClone(foo);
            Assert.AreEqual(0, bar.IgnoreDirect, "IgnoreDirect");
            Assert.AreEqual(0, bar.IgnoreIndirect, "IgnoreIndirect");
            Assert.AreEqual(foo.IncludeDirect, bar.IncludeDirect, "IncludeDirect");
            Assert.AreEqual(foo.IncludeIndirect, bar.IncludeIndirect, "IncludeIndirect");
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields, ImplicitFirstTag = 4)]
        [ProtoPartialIgnore("g_ignoreIndirect")]
        public class ImplicitFieldPOCO
        {
            public event EventHandler Foo;
            public Action Bar;

            public int D_public;

            private int e_private;
            public int E_private
            {
                get { return e_private; }
                set { e_private = value;}
            }

            [ProtoIgnore]
            private int f_ignoreDirect;
            public int F_ignoreDirect
            {
                get { return f_ignoreDirect; }
                set { f_ignoreDirect = value; }
            }

            private int g_ignoreIndirect;
            public int G_ignoreIndirect
            {
                get { return g_ignoreIndirect; }
                set { g_ignoreIndirect = value; }
            }
            [NonSerialized]
            public int H_nonSerialized;

            [ProtoMember(1)]
            private int x_explicitField;

            public int X_explicitField
            {
                get { return x_explicitField; }
                set { x_explicitField = value; }
            }

            [ProtoIgnore]
            private int z_explicitProperty;
            [ProtoMember(2)]
            public int Z_explicitProperty
            {
                get { return z_explicitProperty; }
                set { z_explicitProperty = value; }
            }

        }

        [Test]
        public void TestAllFields()
        {
            ImplicitFieldPOCO foo = new ImplicitFieldPOCO
                                    {
                                        D_public = 100,
                                        E_private = 101,
                                        F_ignoreDirect = 102,
                                        G_ignoreIndirect = 103,
                                        H_nonSerialized = 104,
                                        X_explicitField = 105,
                                        Z_explicitProperty = 106

                                    };
            Assert.AreEqual(100, foo.D_public, "D: pre");
            Assert.AreEqual(101, foo.E_private, "E: pre");
            Assert.AreEqual(102, foo.F_ignoreDirect, "F: pre");
            Assert.AreEqual(103, foo.G_ignoreIndirect, "G: pre");
            Assert.AreEqual(104, foo.H_nonSerialized, "H: pre");
            Assert.AreEqual(105, foo.X_explicitField, "X: pre");
            Assert.AreEqual(106, foo.Z_explicitProperty, "Z: pre");

            ImplicitFieldPOCO bar = Serializer.DeepClone(foo);
            Assert.AreEqual(100, bar.D_public, "D: post");
            Assert.AreEqual(101, bar.E_private, "E: post");
            Assert.AreEqual(0, bar.F_ignoreDirect, "F: post");
            Assert.AreEqual(0, bar.G_ignoreIndirect, "G: post");
            Assert.AreEqual(0, bar.H_nonSerialized, "H: post");
            Assert.AreEqual(105, bar.X_explicitField, "X: post");
            Assert.AreEqual(106, bar.Z_explicitProperty, "Z: post");

            ImplicitFieldPOCOEquiv equiv = Serializer.ChangeType<ImplicitFieldPOCO, ImplicitFieldPOCOEquiv>(foo);
            Assert.AreEqual(100, equiv.D, "D: equiv");
            Assert.AreEqual(101, equiv.E, "E: equiv");
            Assert.AreEqual(105, equiv.X, "X: equiv");
            Assert.AreEqual(106, equiv.Z, "Z: equiv");


        }

        [ProtoContract]
        public class ImplicitFieldPOCOEquiv
        {
            [ProtoMember(4)]
            public int D { get; set;}
            [ProtoMember(5)]
            public int E { get; set;}
            [ProtoMember(1)]
            public int X { get; set;}
            [ProtoMember(2)]
            public int Z { get; set;}
        }


        [Test]
        public void TestAllPublic()
        {
            ImplicitPublicPOCO foo = new ImplicitPublicPOCO
                                     { ImplicitField = 101, ExplicitNonPublic = 102, IgnoreDirect = 103,
                                     IgnoreIndirect = 104, ImplicitNonPublic = 105, ImplicitProperty = 106};

            Assert.AreEqual(101, foo.ImplicitField, "ImplicitField: pre");
            Assert.AreEqual(102, foo.ExplicitNonPublic, "ExplicitNonPublic: pre");
            Assert.AreEqual(103, foo.IgnoreDirect, "IgnoreDirect: pre");
            Assert.AreEqual(104, foo.IgnoreIndirect, "IgnoreIndirect: pre");
            Assert.AreEqual(105, foo.ImplicitNonPublic, "ImplicitNonPublic: pre");
            Assert.AreEqual(106, foo.ImplicitProperty, "ImplicitProperty: pre");

            ImplicitPublicPOCO bar = Serializer.DeepClone(foo);

            Assert.AreEqual(101, bar.ImplicitField, "ImplicitField: post");
            Assert.AreEqual(102, bar.ExplicitNonPublic, "ExplicitNonPublic: post");
            Assert.AreEqual(0, bar.IgnoreDirect, "IgnoreDirect: post");
            Assert.AreEqual(0, bar.IgnoreIndirect, "IgnoreIndirect: post");
            Assert.AreEqual(0, bar.ImplicitNonPublic, "ImplicitNonPublic: post");
            Assert.AreEqual(106, bar.ImplicitProperty, "ImplicitProperty: post");

            ImplicitPublicPOCOEquiv equiv = Serializer.ChangeType<ImplicitPublicPOCO, ImplicitPublicPOCOEquiv>(foo);
            Assert.AreEqual(101, equiv.ImplicitField, "ImplicitField: equiv");
            Assert.AreEqual(102, equiv.ExplicitNonPublic, "ExplicitNonPublic: equiv");
            Assert.AreEqual(106, equiv.ImplicitProperty, "ImplicitProperty: equiv");
        }

        [ProtoContract]
        public class ImplicitPublicPOCOEquiv
        {
            [ProtoMember(4)]
            public int ImplicitField { get; set; }

            [ProtoMember(1)]
            public int ExplicitNonPublic { get; set; }

            [ProtoMember(5)]
            public int ImplicitProperty { get; set; }

        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, ImplicitFirstTag = 4)]
        [ProtoPartialIgnore("IgnoreIndirect")]
        public class ImplicitPublicPOCO
        {
            internal int ImplicitNonPublic { get; set;}
            [ProtoMember(1)]
            internal int ExplicitNonPublic { get; set; }

            public int ImplicitField;

            public int ImplicitProperty { get; set;}

            [ProtoIgnore]
            public int IgnoreDirect { get; set; }

            public int IgnoreIndirect { get; set; }
        }

    }
}
