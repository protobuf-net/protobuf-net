#if !NO_NHIBERNATE
using NHibernate.Proxy;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;
using NHibernate.Engine;

namespace ProtoBuf.unittest.ThirdParty
{
    public class NHibernateProxies
    {
        public class FooWrapper
        {
            public Foo Foo { get; set; }
        }
        public class Foo
        {
            public virtual int Id { get; set; }
            public virtual string Name { get; set; }
        }
        public class FooProxy : Foo, INHibernateProxy, ILazyInitializer
        {
            public override int Id
            {
                get { return wrapped.Id; }
                set { wrapped.Id = value; }
            }
            public override string Name
            {
                get { return wrapped.Name; }
                set { wrapped.Name = value; }
            }
            private readonly Foo wrapped;
            public FooProxy(Foo wrapped) { this.wrapped = wrapped; }
            ILazyInitializer INHibernateProxy.HibernateLazyInitializer
            {
                get { return this; }
            }

            #region ILazyInitializer Members

            string ILazyInitializer.EntityName
            {
                get { throw new NotImplementedException(); }
            }

            object ILazyInitializer.GetImplementation(NHibernate.Engine.ISessionImplementor s)
            {
                return wrapped;
            }

            object ILazyInitializer.GetImplementation()
            {
                return wrapped;
            }

            object ILazyInitializer.Identifier
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            void ILazyInitializer.Initialize()
            {
                throw new NotImplementedException();
            }

            bool ILazyInitializer.IsUninitialized
            {
                get { throw new NotImplementedException(); }
            }

            Type ILazyInitializer.PersistentClass
            {
                get { throw new NotImplementedException(); }
            }

            NHibernate.Engine.ISessionImplementor ILazyInitializer.Session
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            void ILazyInitializer.SetImplementation(object target)
            {
                throw new NotImplementedException();
            }

            public void SetSession(ISessionImplementor s) { }

            public void UnsetSession() { }

            bool ILazyInitializer.Unwrap
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            bool ILazyInitializer.IsReadOnlySettingAvailable => true;

            bool ILazyInitializer.ReadOnly { get; set; }

            #endregion
        }

        static RuntimeTypeModel BuildModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(FooWrapper), false).Add("Foo");
            model.Add(typeof(Foo), false).Add("Id", "Name");
            return model;
        }

        class Bar : Foo { }
        [Fact]
        public void AttemptToSerializeUnknownSubtypeShouldFail_Runtime()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var model = BuildModel();
                model.Serialize(Stream.Null, new Bar());
            });
        }

        [Fact]
        public void AttemptToSerializeUnknownSubtypeShouldFail_CompileInPlace()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var model = BuildModel();
                model.CompileInPlace();
                model.Serialize(Stream.Null, new Bar());
            });
        }
        [Fact]
        public void AttemptToSerializeUnknownSubtypeShouldFail_Compile()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var model = BuildModel().Compile();
                model.Serialize(Stream.Null, new Bar());
            });
        }

        [Fact]
        public void AttemptToSerializeWrapperUnknownSubtypeShouldFail_Runtime()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var model = BuildModel();
                model.Serialize(Stream.Null, new FooWrapper { Foo = new Bar() });
            });
        }

        [Fact]
        public void AttemptToSerializeWrapperUnknownSubtypeShouldFail_CompileInPlace()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var model = BuildModel();
                model.CompileInPlace();
                model.Serialize(Stream.Null, new FooWrapper { Foo = new Bar() });
            });
        }

        [Fact]
        public void AttemptToSerializeWrapperUnknownSubtypeShouldFail_Compile()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var model = BuildModel().Compile();
                model.Serialize(Stream.Null, new FooWrapper { Foo = new Bar() });
            });
        }

        [Fact]
        public void CanRoundTripProxyToRegular()
        {
            var model = BuildModel();

            Foo foo = new Foo { Id = 1234, Name = "abcd" }, proxy = new FooProxy(foo), clone;
            Assert.NotNull(foo);
            Assert.NotNull(proxy);

            clone = (Foo)model.DeepClone(foo);
            CompareFoo(foo, clone, "Runtime/Foo");
            clone = (Foo)model.DeepClone(proxy);
            CompareFoo(proxy, clone, "Runtime/FooProxy");

            model.CompileInPlace();
            clone = (Foo)model.DeepClone(foo);
            CompareFoo(foo, clone, "CompileInPlace/Foo");
            clone = (Foo)model.DeepClone(proxy);
            CompareFoo(proxy, clone, "CompileInPlace/FooProxy");

            var compiled = model.Compile();
            clone = (Foo)compiled.DeepClone(foo);
            CompareFoo(foo, clone, "Compile/Foo");
            clone = (Foo)compiled.DeepClone(proxy);
            CompareFoo(proxy, clone, "Compile/FooProxy");

        }

#pragma warning disable IDE0060 // Remove unused parameter
        private static void CompareFoo(Foo original, Foo clone, string message)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            Assert.NotNull(clone); //, message);
            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone.Id); //, message + ": Id");
            Assert.Equal(original.Name, clone.Name); //, message + ": Name");
            Assert.Equal(typeof(Foo), clone.GetType()); //, message);
        }
    }
}
#endif