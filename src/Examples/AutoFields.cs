using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Xunit;
using System.Reflection;
using ProtoBuf.Meta;

namespace Examples
{
    
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

        [Fact]
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
            Assert.Equal(0, bar.IgnoreDirect); //, "IgnoreDirect");
            Assert.Equal(0, bar.IgnoreIndirect); //, "IgnoreIndirect");
            Assert.Equal(foo.IncludeDirect, bar.IncludeDirect); //, "IncludeDirect");
            Assert.Equal(foo.IncludeIndirect, bar.IncludeIndirect); //, "IncludeIndirect");
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields, ImplicitFirstTag = 4)]
        [ProtoPartialIgnore("g_ignoreIndirect")]
        public class ImplicitFieldPOCO
        {
            public event EventHandler Foo;
            protected virtual void OnFoo()
            {
                if (Foo != null) Foo(this, EventArgs.Empty);
            }
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

        [Fact]
        public void CanDetectNonSerializedAttribute()
        {
            const bool inherit = false;
            var field = typeof(ImplicitFieldPOCO).GetField(nameof(ImplicitFieldPOCO.H_nonSerialized));
#if COREFX
            Attribute[] all = System.Linq.Enumerable.ToArray(field.GetCustomAttributes(inherit));
#else
            Attribute[] all = field.GetCustomAttributes(inherit).Cast<Attribute>().ToArray();
#endif
            bool hasNonSerialized = all.Any(x => x.GetType().FullName == "System.NonSerializedAttribute");

            Assert.True(hasNonSerialized); // we can detect it in regular .net, though
        }
        [Fact]
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
            Assert.Equal(100, foo.D_public); //, "D: pre");
            Assert.Equal(101, foo.E_private); //, "E: pre");
            Assert.Equal(102, foo.F_ignoreDirect); //, "F: pre");
            Assert.Equal(103, foo.G_ignoreIndirect); //, "G: pre");
            Assert.Equal(104, foo.H_nonSerialized); //, "H: pre");
            Assert.Equal(105, foo.X_explicitField); //, "X: pre");
            Assert.Equal(106, foo.Z_explicitProperty); //, "Z: pre");

            ImplicitFieldPOCO bar = Serializer.DeepClone(foo);
            Assert.Equal(100, bar.D_public); //, "D: post");
            Assert.Equal(101, bar.E_private); //, "E: post");
            Assert.Equal(0, bar.F_ignoreDirect); //, "F: post");
            Assert.Equal(0, bar.G_ignoreIndirect); //, "G: post");
//#if COREFX
//            Assert.Equal(104, bar.H_nonSerialized); //, "H: post");
//#else
            Assert.Equal(0, bar.H_nonSerialized); //, "H: post");
//#endif
            Assert.Equal(105, bar.X_explicitField); //, "X: post");
            Assert.Equal(106, bar.Z_explicitProperty); //, "Z: post");

            ImplicitFieldPOCOEquiv equiv = Serializer.ChangeType<ImplicitFieldPOCO, ImplicitFieldPOCOEquiv>(foo);
//#if COREFX // change in H being serialized/not moves everything around
//            Assert.Equal(100, equiv.D); //, "D: equiv");
//            Assert.Equal(104, equiv.E); //, "E: equiv");
//            Assert.Equal(105, equiv.X); //, "X: equiv");
//            Assert.Equal(106, equiv.Z); //, "Z: equiv");
//#else
            Assert.Equal(100, equiv.D); //, "D: equiv");
            Assert.Equal(101, equiv.E); //, "E: equiv");
            Assert.Equal(105, equiv.X); //, "X: equiv");
            Assert.Equal(106, equiv.Z); //, "Z: equiv");
//#endif
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


        [Fact]
        public void TestAllPublic()
        {
            ImplicitPublicPOCO foo = new ImplicitPublicPOCO
                                     { ImplicitField = 101, ExplicitNonPublic = 102, IgnoreDirect = 103,
                                     IgnoreIndirect = 104, ImplicitNonPublic = 105, ImplicitProperty = 106};

            Assert.Equal(101, foo.ImplicitField); //, "ImplicitField: pre");
            Assert.Equal(102, foo.ExplicitNonPublic); //, "ExplicitNonPublic: pre");
            Assert.Equal(103, foo.IgnoreDirect); //, "IgnoreDirect: pre");
            Assert.Equal(104, foo.IgnoreIndirect); //, "IgnoreIndirect: pre");
            Assert.Equal(105, foo.ImplicitNonPublic); //, "ImplicitNonPublic: pre");
            Assert.Equal(106, foo.ImplicitProperty); //, "ImplicitProperty: pre");

            ImplicitPublicPOCO bar = Serializer.DeepClone(foo);

            Assert.Equal(101, bar.ImplicitField); //, "ImplicitField: post");
            Assert.Equal(102, bar.ExplicitNonPublic); //, "ExplicitNonPublic: post");
            Assert.Equal(0, bar.IgnoreDirect); //, "IgnoreDirect: post");
            Assert.Equal(0, bar.IgnoreIndirect); //, "IgnoreIndirect: post");
            Assert.Equal(0, bar.ImplicitNonPublic); //, "ImplicitNonPublic: post");
            Assert.Equal(106, bar.ImplicitProperty); //, "ImplicitProperty: post");

            ImplicitPublicPOCOEquiv equiv = Serializer.ChangeType<ImplicitPublicPOCO, ImplicitPublicPOCOEquiv>(foo);
            Assert.Equal(101, equiv.ImplicitField); //, "ImplicitField: equiv");
            Assert.Equal(102, equiv.ExplicitNonPublic); //, "ExplicitNonPublic: equiv");
            Assert.Equal(106, equiv.ImplicitProperty); //, "ImplicitProperty: equiv");
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
