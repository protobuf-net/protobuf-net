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
            var model = TypeModel.Create();
            var member = model[typeof(Foo)][1];
            Assert.Equal(typeof(Bar), member.ItemType);
            Assert.Equal(typeof(List<Bar>), member.DefaultType);
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
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
                ms.Position = 0;
                var clone = (FooEnumerable)ser.ReadObject(ms);
                Assert.NotNull(clone.Bars);
                Assert.Single(clone.Bars);
            }
        }
        [Fact]
        public void XmlSerializer_DoesntSupportNakedEnumerables()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var ser = new XmlSerializer(typeof(FooEnumerable));
                using (var ms = new MemoryStream())
                {
                    ser.Serialize(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
                    ms.Position = 0;
                    var clone = (FooEnumerable)ser.Deserialize(ms);
                    Assert.NotNull(clone.Bars);
                    Assert.Single(clone.Bars);
                }
            });
        }
#if !COREFX
        [Fact]
        public void JavaScriptSerializer_DoesSupportNakedEnumerables()
        {
            var ser = new JavaScriptSerializer();
            using (var ms = new MemoryStream())
            {
                string s = ser.Serialize(new FooEnumerable { Bars = new[] { new Bar { } } });
                ms.Position = 0;
                var clone = (FooEnumerable)ser.Deserialize(s, typeof(FooEnumerable));
                Assert.NotNull(clone.Bars);
                Assert.Single(clone.Bars);
            }
        }
#endif

        [Fact]
        public void ProtobufNet_DoesSupportNakedEnumerables()
        {
            var ser = TypeModel.Create();
            using (var ms = new MemoryStream())
            {
                ser.Serialize(ms, new FooEnumerable { Bars = new[] { new Bar { } } });
                ms.Position = 0;
                var clone = (FooEnumerable)ser.Deserialize(ms, null, typeof(FooEnumerable));
                Assert.NotNull(clone.Bars);
                Assert.Single(clone.Bars);
            }
        }

        // see https://gist.github.com/gmcelhanon/5391894
        [Serializable, ProtoContract]
        public class GoalPlanningModel1
        {
            [ProtoMember(1)]
            public IEnumerable<ProposedGoal> ProposedGoals { get; set; }

            [ProtoMember(2)]
            public IEnumerable<PublishedGoal> PublishedGoals { get; set; }
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
        public void TestPlanningModelWithEnumerables()
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
            var dll = model.Compile("TestPlanningModelWithEnumerables", "TestPlanningModelWithEnumerables.dll");
            Verify(obj, dll, "dll");
            PEVerify.AssertValid("TestPlanningModelWithEnumerables.dll");
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

        private static void Verify(GoalPlanningModel2 obj, TypeModel model, string caption)
        {
            var clone = (GoalPlanningModel2)model.DeepClone(obj);
            Assert.Null(clone.PublishedGoals); //, caption + ":published");
            Assert.NotNull(clone.ProposedGoals); //, caption + ":proposed");
            Assert.Equal(1, clone.ProposedGoals.Count); //, caption + ":count");
            Assert.Equal(23, clone.ProposedGoals[0].X); //, caption + ":X");
        }
        private static void Verify(GoalPlanningModel1 obj, TypeModel model, string caption)
        {
            var clone = (GoalPlanningModel1)model.DeepClone(obj);
            Assert.Null(clone.PublishedGoals); //, caption + ":published");
            Assert.NotNull(clone.ProposedGoals); //, caption + ":proposed");
            Assert.Single(clone.ProposedGoals); //, caption + ":count");
            Assert.Equal(23, clone.ProposedGoals.Single().X); //, caption + ":X");
        }
    }
}
