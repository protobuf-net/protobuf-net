using ProtoBuf.Meta;
using System;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue588
    {
        [Fact(Skip = "concurrency means that this is *really* harmful to the other tests")]
        public void CanSwapDefaultModel()
        {
            var oldModel = RuntimeTypeModel.Default;
            try
            {
                var newModel = RuntimeTypeModel.Create();
                newModel.MakeDefault();

                Assert.Same(newModel, RuntimeTypeModel.Default);
            }
            finally
            {
                oldModel.MakeDefault();
            }
        }

        [Fact]
        public void CannotSwapDefaultModel_Frozen()
        {
            var oldModel = RuntimeTypeModel.Default;
            try
            {
                var newModel = RuntimeTypeModel.Create();
                newModel.Freeze();

                var ex = Assert.Throws<InvalidOperationException>(() => newModel.MakeDefault());
                Assert.Equal("The default model cannot be frozen", ex.Message);

                Assert.Same(oldModel, RuntimeTypeModel.Default);
            }
            finally
            {
                oldModel.MakeDefault();
            }
        }

        [Fact]
        public void CannotSwapDefaultModel_AutoAddDisabled()
        {
            var oldModel = RuntimeTypeModel.Default;
            try
            {
                var newModel = RuntimeTypeModel.Create();
                newModel.AutoAddMissingTypes = false;

                var ex = Assert.Throws<InvalidOperationException>(() => newModel.MakeDefault());
                Assert.Equal("The default model must allow missing types", ex.Message);

                Assert.Same(oldModel, RuntimeTypeModel.Default);
            }
            finally
            {
                oldModel.MakeDefault();
            }
        }

        [Fact]
        public void CannotSwapDefaultModel_ImplicitZeroDisabled()
        {
            var oldModel = RuntimeTypeModel.Default;
            try
            {
                var newModel = RuntimeTypeModel.Create();
                newModel.UseImplicitZeroDefaults = false;

                var ex = Assert.Throws<InvalidOperationException>(() => newModel.MakeDefault());
                Assert.Equal("UseImplicitZeroDefaults cannot be disabled on the default model", ex.Message);

                Assert.Same(oldModel, RuntimeTypeModel.Default);
            }
            finally
            {
                oldModel.MakeDefault();
            }
        }
    }
}
