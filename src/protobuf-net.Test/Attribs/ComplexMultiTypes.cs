using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.IO;
using ProtoBuf.unittest.Meta;

namespace ProtoBuf.unittest.Attribs
{
    [TestFixture]
    public class ComplexMultiTypes
    {
        #region DTO
        [ProtoContract]
        [ProtoInclude(10000, typeof(EntityDTO))]
        public class ComponentContainerDTO
        {
            [ProtoMember(1)]
            public IList<ComponentDTO> Components { get; set; }

            public ComponentContainerDTO()
            {
                this.Components = new List<ComponentDTO>();
            }
        }

        [ProtoContract]
        public class EntityDTO : ComponentContainerDTO
        {
            [ProtoMember(1)]
            public int Id { get; set; }

        }

        [ProtoContract]
        [ProtoInclude(10001, typeof(HealthDTO))]
        [ProtoInclude(10002, typeof(PhysicalLocationDTO))]
        public class ComponentDTO //: MyDynamicObjectDTO
        {
            public EntityDTO Owner { get; set; }
            [ProtoMember(2)]
            public int Id { get; set; }
            [ProtoMember(3)]
            public string Name { get; set; }

        }

        [ProtoContract]
        public class HealthDTO : ComponentDTO
        {
            [ProtoMember(1)]
            public decimal CurrentHealth { get; set; }

        }

        [ProtoContract]
        public class PhysicalLocationDTO : ComponentDTO
        {
            [ProtoMember(1)]
            public decimal X { get; set; }
            [ProtoMember(2)]
            public decimal Y { get; set; }
        }


        
        #endregion

        private static RuntimeTypeModel BuildModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(ComponentContainerDTO), true);
            model.Add(typeof(ComponentDTO), true);
            /*
            model.Add(typeof(ComponentContainerDTO), false)
                .Add("Components");
            model.Add(typeof(EntityDTO), false)
                .Add(1, "Id");
            model[typeof(ComponentContainerDTO)].AddSubType(10000, typeof(EntityDTO));
            model.Add(typeof(ComponentDTO), false)
                .Add(2, "Id")
                .Add(3, "Name");
            model.Add(typeof(HealthDTO), false)
                .Add(1, "CurrentHealth");
            model.Add(typeof(PhysicalLocationDTO), false)
                .Add(1, "X")
                .Add(2, "Y");
            model[typeof(ComponentDTO)].AddSubType(10001, typeof(HealthDTO));
            model[typeof(ComponentDTO)].AddSubType(10002, typeof(PhysicalLocationDTO));*/
            return model;


        }

        [Test]
        public void RoundtripEmptyEntityDto()
        {
            var model = BuildModel();
            CheckEmptyEntityDto(model, "Runtime");

            model.CompileInPlace();
            CheckEmptyEntityDto(model, "CompileInPlace");

            CheckEmptyEntityDto(model.Compile(), "Compile");

        }

        private static void CheckEmptyEntityDto(TypeModel model, string message) {
            // Test 1 - simple case, EntityDTO only
            var memstream = new MemoryStream();
            model.Serialize(memstream, new EntityDTO() { Id = 1 });
            memstream.Seek(0, SeekOrigin.Begin);
            var result = (EntityDTO)model.Deserialize(memstream, null, typeof(EntityDTO));

            Assert.AreEqual(typeof(EntityDTO), result.GetType(), message + ":type");
            Assert.AreEqual(1, result.Id, message + ":Id");
            Assert.AreEqual(0, result.Components.Count, message + ":Count");
        }

        [Test]
        public void CanCompileModel()
        {
            BuildModel().Compile("ComplexMultiTypes", "ComplexMultiTypes.dll");
            PEVerify.Verify("ComplexMultiTypes.dll");
        }

        [Test]
        public void RoundtripEntityDtoWithItems()
        {
            var model = BuildModel();
            CheckEntityDtoWithItems(model, "Runtime");
            
            model.CompileInPlace();
            CheckEntityDtoWithItems(model, "CompileInPlace");

            CheckEntityDtoWithItems(model.Compile(), "Compile");

        }
        private static void CheckEntityDtoWithItems(TypeModel model, string message) {
            var entity = new EntityDTO() { Id = 1 };
            var healthComponent = new HealthDTO() { CurrentHealth = 100, Owner = entity, Name = "Health", Id = 2 };
            entity.Components.Add(healthComponent);
            var locationComponent = new PhysicalLocationDTO() { X = 1, Y = 2, Owner = entity, Name = "PhysicalLocation", Id = 3 };
            entity.Components.Add(locationComponent);

            MemoryStream memstream2 = new MemoryStream();
            model.Serialize(memstream2, entity);
            memstream2.Seek(0, SeekOrigin.Begin);
            var result2 = (EntityDTO)model.Deserialize(memstream2, null, typeof(EntityDTO));

            Assert.AreEqual(1, result2.Id, message + ":Id");
            Assert.AreEqual(2, result2.Components.Count, message + ":Count");
            // These two tests are lame and will not be used for long
            Assert.AreEqual(typeof(HealthDTO), result2.Components.First().GetType(), message + ":First");
            Assert.AreEqual(typeof(PhysicalLocationDTO), result2.Components.Last().GetType(), message + ":Last");
        }
    }
}
