using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
#if !COREFX
using System.Web.Script.Serialization;
#endif
using System.Xml.Serialization;

namespace Examples.Issues
{
    
    public class SO7793527
    {
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public IList<Bar> Bars { get; set; }
        }

        [DataContract, ProtoContract]
        public class FooEnumerable
        {
            [ProtoMember(1), DataMember(Order=1)]
            public IEnumerable<Bar> Bars { get; set; }
        }


        [DataContract, ProtoContract]
        public class Bar
        {

        }

        [Fact]
        public void AutoConfigOfModel()
        {
            var model = RuntimeTypeModel.Create();
            var member = model[typeof(Foo)][1];
            Assert.Equal(typeof(Bar), member.ItemType);
            Assert.Equal(typeof(IList<Bar>), member.DefaultType);
        }
        [Fact]
        public void DefaultToListT()
        {
            var obj = new Foo { Bars = new Bar[] { new Bar { }, new Bar { } } };

            var clone = Serializer.DeepClone(obj);
            Assert.Equal(2, clone.Bars.Count);
            Assert.IsType<List<Bar>>(clone.Bars);
        }

        [Fact]
        public void DataContractSerializer_DoesSupportNakedEnumerables()
        {
            var ser = new DataContractSerializer(typeof(FooEnumerable));
            using var ms = new MemoryStream();
            ser.WriteObject(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
            ms.Position = 0;
            var clone = (FooEnumerable)ser.ReadObject(ms);
            Assert.NotNull(clone.Bars);
            Assert.Single(clone.Bars);
        }
        [Fact]
        public void XmlSerializer_DoesntSupportNakedEnumerables()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var ser = new XmlSerializer(typeof(FooEnumerable));
                using var ms = new MemoryStream();
                ser.Serialize(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
                ms.Position = 0;
                var clone = (FooEnumerable)ser.Deserialize(ms);
                Assert.NotNull(clone.Bars);
                Assert.Single(clone.Bars);
            });
        }
#if !COREFX
        [Fact]
        public void JavaScriptSerializer_DoesSupportNakedEnumerables()
        {
            var ser = new JavaScriptSerializer();
            using var ms = new MemoryStream();
            string s = ser.Serialize(new FooEnumerable { Bars = new[] { new Bar { } } });
            ms.Position = 0;
            var clone = (FooEnumerable)ser.Deserialize(s, typeof(FooEnumerable));
            Assert.NotNull(clone.Bars);
            Assert.Single(clone.Bars);
        }
#endif

        [Fact]
        public void ProtobufNet_SupportsNakedEnumerables()
        {
            var ser = RuntimeTypeModel.Create();
            using var ms = new MemoryStream();
            ser.Serialize(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
            ms.Position = 0;
            var clone = (FooEnumerable)ser.Deserialize(ms, null, typeof(FooEnumerable));
            Assert.NotNull(clone.Bars);
            Assert.Single(clone.Bars);
        }

        [Fact]
        public void ProtobufNet_SupportsNakedEnumerables_ButMustBeAddable()
        {
            var ser = RuntimeTypeModel.Create();
            using var ms = new MemoryStream();
            ser.Serialize(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
            ms.Position = 0;
            // let's make Bars non-null in the target object, with something immutable
            // (an empty array), to see how it goes boom
            var obj = new FooEnumerable { Bars = Array.Empty<Bar>() };
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var clone = (FooEnumerable)ser.Deserialize(ms, obj, type: typeof(FooEnumerable));
            });
            Assert.Equal("For repeated data declared as System.Collections.Generic.IEnumerable`1[Examples.Issues.SO7793527+Bar], the *underlying* collection (Examples.Issues.SO7793527+Bar[]) must implement ICollection<T> and must not declare itself read-only; alternative (more exotic) collections can be used, but must be declared using their well-known form (for example, a member could be declared as ImmutableHashSet<T>)", ex.Message);
        }

        // see https://gist.github.com/gmcelhanon/5391894
        [Serializable, ProtoContract]
        public class GoalPlanningModel1
        {
            [ProtoMember(1)]
            public ICollection<ProposedGoal> ProposedGoals { get; set; }

            [ProtoMember(2)]
            public ICollection<PublishedGoal> PublishedGoals { get; set; }
        }

        // In order to get protobuf-net to serialize it, I have to change the IEnumerabe<T> members to IList<T>.

        [Serializable, ProtoContract]
        public class GoalPlanningModel2
        {
            [ProtoMember(1)]
            public IList<ProposedGoal> ProposedGoals { get; set; }

            [ProtoMember(2)]
            public IList<PublishedGoal> PublishedGoals { get; set; }
        }
        [ProtoContract]
        public class ProposedGoal { [ProtoMember(1)] public int X { get; set; } }
        [ProtoContract]
        public class PublishedGoal { [ProtoMember(1)] public int X { get; set; } }

        [Fact]
        public void TestPlanningModelWithCollections()
        {
            var obj = new GoalPlanningModel1
            {
                ProposedGoals = new List<ProposedGoal> { new ProposedGoal { X = 23 } }
            };
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Verify(obj, model, "Runtime");
            model.CompileInPlace();
            Verify(obj, model, "CompileInPlace");
            Verify(obj, model.Compile(), "Compile");
            var dll = model.Compile("TestPlanningModelWithCollections", "TestPlanningModelWithCollections.dll");
            Verify(obj, dll, "dll");
            PEVerify.AssertValid("TestPlanningModelWithCollections.dll");
        }

        [Fact]
        public void TestPlanningModelWithLists()
        {
            var obj = new GoalPlanningModel2
            {
                ProposedGoals = new List<ProposedGoal> {new ProposedGoal { X = 23 }}
            };
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Verify(obj, model, "Runtime");
            model.CompileInPlace();
            Verify(obj, model, "CompileInPlace");
            Verify(obj, model.Compile(), "Compile");
            var dll = model.Compile("TestPlanningModelWithLists", "TestPlanningModelWithLists.dll");
            Verify(obj, dll, "dll");
            PEVerify.AssertValid("TestPlanningModelWithLists.dll");
        }

#pragma warning disable IDE0060
        private static void Verify(GoalPlanningModel2 obj, TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            var clone = (GoalPlanningModel2)model.DeepClone(obj);
            Assert.Null(clone.PublishedGoals); //, caption + ":published");
            Assert.NotNull(clone.ProposedGoals); //, caption + ":proposed");
            Assert.Equal(1, clone.ProposedGoals.Count); //, caption + ":count");
            Assert.Equal(23, clone.ProposedGoals[0].X); //, caption + ":X");
        }

#pragma warning disable IDE0060
        private static void Verify(GoalPlanningModel1 obj, TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            var clone = (GoalPlanningModel1)model.DeepClone(obj);
            Assert.Null(clone.PublishedGoals); //, caption + ":published");
            Assert.NotNull(clone.ProposedGoals); //, caption + ":proposed");
            Assert.Single(clone.ProposedGoals); //, caption + ":count");
            Assert.Equal(23, clone.ProposedGoals.Single().X); //, caption + ":X");
        }
    }
}
