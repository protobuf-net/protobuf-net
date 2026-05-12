using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace Examples;

public class SurrogateTests
{
    public class BaseClass(int id)
    {
        public int Id { get; private set; } = id;

        public void SetId(int value) => Id = value; // this only exists for Compile mode
    }

    public class DerivedClass(int id, int number) : BaseClass(id)
    {
        public int Number { get; private set; } = number;

        public void SetNumber(int value) => Number = value; // this only exists for Compile mode
    }

    [ProtoContract]
    [ProtoInclude(1001, typeof(DerivedClassSurrogate))]
    public class BaseClassSurrogate
    {
        [ProtoMember(1)] public int Id { get; set; }

        [OnDeserializing]
        public virtual void BeforeDeserialize()
        {
        }

        public static implicit operator BaseClassSurrogate(BaseClass value) => value switch
        {
            null => null,
            DerivedClass derived => new DerivedClassSurrogate() { Id = derived.Id, Number = derived.Number },
            _ => new BaseClassSurrogate() { Id = value.Id },
        };

        public static implicit operator BaseClass(BaseClassSurrogate value) => value switch
        {
            null => null,
            DerivedClassSurrogate derived => new DerivedClass(derived.Id, derived.Number),
            _ => new BaseClass(value.Id),
        };
    }

    [ProtoContract]
    public class DerivedClassSurrogate : BaseClassSurrogate
    {
        [ProtoMember(1)] public int Number { get; set; }

        public static implicit operator DerivedClassSurrogate(DerivedClass value)
            => value is null ? null : new DerivedClassSurrogate() { Id = value.Id, Number = value.Number };

        public static implicit operator DerivedClass(DerivedClassSurrogate value)
            => value is null ? null : new DerivedClass(value.Id, value.Number);
    }

    public enum TestMode
    {
        Runtime,
        CompileInPlace,
        CompileInMemory,
        CompileToFile,
    }

    [Theory]
    [InlineData(false, false, TestMode.Runtime)]
    [InlineData(true, false, TestMode.Runtime)]
    [InlineData(false, false, TestMode.CompileInPlace)]
    [InlineData(true, false, TestMode.CompileInPlace)]
    [InlineData(false, false, TestMode.CompileInMemory)]
    [InlineData(true, false, TestMode.CompileInMemory)]
#if NETFX || NET9_0_OR_GREATER
    [InlineData(false, false, TestMode.CompileToFile)]
    [InlineData(true, false, TestMode.CompileToFile)]
#endif
    [InlineData(false, true, TestMode.Runtime)]
    [InlineData(true, true, TestMode.Runtime)]
    [InlineData(false, true, TestMode.CompileInPlace)]
    [InlineData(true, true, TestMode.CompileInPlace)]
    [InlineData(false, true, TestMode.CompileInMemory)]
    [InlineData(true, true, TestMode.CompileInMemory)]
#if NETFX || NET9_0_OR_GREATER
    [InlineData(false, true, TestMode.CompileToFile)]
    [InlineData(true, true, TestMode.CompileToFile)]
#endif
    public void ExecuteWithoutInheritance(bool baseSurrogate, bool derivedSurrogate, TestMode mode)
    {
        RuntimeTypeModel model = RuntimeTypeModel.Create();
        model.AutoCompile = false;

        model.Add(typeof(BaseClassSurrogate), false).AddField(1, nameof(BaseClassSurrogate.Id));
        model.Add(typeof(DerivedClassSurrogate), false).AddField(1, nameof(DerivedClassSurrogate.Number));
        var baseType = model.Add(typeof(BaseClass), false);
        var derivedType = model.Add(typeof(DerivedClass), false);


        if (baseSurrogate)
        {
            baseType.SetSurrogate(typeof(BaseClassSurrogate));
        }
        else
        {
            baseType.UseConstructor = false;
            baseType.AddField(1, nameof(BaseClass.Id));
        }

        if (derivedSurrogate)
        {
            derivedType.SetSurrogate(typeof(DerivedClassSurrogate));
        }
        else
        {
            derivedType.UseConstructor = false;
            derivedType.AddField(1, nameof(DerivedClass.Number));
        }

        TypeModel scenario = model;
        switch (mode)
        {
            case TestMode.CompileInPlace:
                model.CompileInPlace();
                break;
            case TestMode.CompileInMemory:
                scenario = model.Compile();
                break;
            case TestMode.CompileToFile:
                int key = (baseSurrogate ? 1 : 0) | (derivedSurrogate ? 2 : 0);
                scenario = model.Compile(new RuntimeTypeModel.CompilerOptions
                {
                    TypeName = "MyModel",
#if (NETFX || NET9_0_OR_GREATER) && !(NET && BACK_COMPAT)
                    OutputPath = $"NoSurrogate_{key}.dll",
#endif
                });
                break;
        }

        ExecuteImpl(scenario);

        static void ExecuteImpl(TypeModel model) // see ExpectedPayloads for proofs
        {
            var a = Assert.IsType<BaseClass>(RoundTrip(model, new BaseClass(1), "08-01"));
            Assert.Equal(1, a.Id);

            var c = Assert.IsType<DerivedClass>(RoundTrip(model, new DerivedClass(4, 5), "08-05"));
            Assert.Equal(0, c.Id);
            Assert.Equal(5, c.Number);
        }
    }

    [Theory]
    [InlineData(false, false, TestMode.Runtime)]
    [InlineData(true, false, TestMode.Runtime)]
    [InlineData(false, false, TestMode.CompileInPlace)]
    [InlineData(true, false, TestMode.CompileInPlace)]
    [InlineData(false, false, TestMode.CompileInMemory)]
    [InlineData(true, false, TestMode.CompileInMemory)]
#if (NETFX || NET9_0_OR_GREATER) && !(NET && BACK_COMPAT)
    [InlineData(false, false, TestMode.CompileToFile)]
    [InlineData(true, false, TestMode.CompileToFile)]
#endif

    // not supported: setting surrogate at derived tier
    [InlineData(false, true, TestMode.Runtime)]
    [InlineData(true, true, TestMode.Runtime)]
    [InlineData(false, true, TestMode.CompileInPlace)]
    [InlineData(true, true, TestMode.CompileInPlace)]
    [InlineData(false, true, TestMode.CompileInMemory)]
    [InlineData(true, true, TestMode.CompileInMemory)]
#if (NETFX || NET9_0_OR_GREATER) && !(NET && BACK_COMPAT)
    [InlineData(false, true, TestMode.CompileToFile)]
    [InlineData(true, true, TestMode.CompileToFile)]
#endif
    public void ExecuteWithInheritance(bool baseSurrogate, bool derivedSurrogate, TestMode mode)
    {
        RuntimeTypeModel model = RuntimeTypeModel.Create();
        model.AutoCompile = false;

        var baseType = model.Add(typeof(BaseClass), false);
        var derivedType = model.Add(typeof(DerivedClass), false);
        baseType.AddSubType(1001, typeof(DerivedClass));

        if (baseSurrogate)
        {
            baseType.SetSurrogate(typeof(BaseClassSurrogate));
        }
        else
        {
            baseType.UseConstructor = false;
            baseType.AddField(1, nameof(BaseClass.Id));
        }

        if (derivedSurrogate)
        {
            derivedType.SetSurrogate(typeof(DerivedClassSurrogate));
        }
        else
        {
            derivedType.UseConstructor = false;
            derivedType.AddField(1, nameof(DerivedClass.Number));
        }

        try
        {
            TypeModel scenario = model;
            switch (mode)
            {
                case TestMode.CompileInPlace:
                    model.CompileInPlace();
                    break;
                case TestMode.CompileInMemory:
                    scenario = model.Compile();
                    break;
                case TestMode.CompileToFile:
                    int key = (baseSurrogate ? 1 : 0) | (derivedSurrogate ? 2 : 0);
                    scenario = model.Compile(new RuntimeTypeModel.CompilerOptions
                    {
                        TypeName = "MyModel",
#if (NETFX || NET9_0_OR_GREATER) && !(NET && BACK_COMPAT)
                        OutputPath = $"Surrogate_{key}.dll",
#endif
                    });
                    break;
            }

            ExecuteImpl(scenario);
            Assert.False(derivedSurrogate, "expected fault");
        }
        catch (InvalidOperationException ex) when (derivedSurrogate)
        {
            Assert.Contains("Surrogate type must refer to the root inheritance type", ex.Message);
        }


        static void ExecuteImpl(TypeModel model) // see ExpectedPayloads for proofs
        {
            var a = Assert.IsType<BaseClass>(RoundTrip(model, new BaseClass(1), "08-01"));
            Assert.Equal(1, a.Id);

            var b = Assert.IsType<DerivedClass>(RoundTrip<BaseClass>(model, new DerivedClass(2, 3),
                "CA-3E-02-08-03-08-02"));
            Assert.Equal(2, b.Id);
            Assert.Equal(3, b.Number);

            var c = Assert.IsType<DerivedClass>(RoundTrip(model, new DerivedClass(4, 5), "CA-3E-02-08-05-08-04"));
            Assert.Equal(4, c.Id);
            Assert.Equal(5, c.Number);
        }
    }

    [Fact]
    public void ExpectedPayloads()
    {
        RoundTrip(RuntimeTypeModel.Default, new BaseClassSurrogate { Id = 1 }, "08-01");
        RoundTrip(RuntimeTypeModel.Default, new DerivedClassSurrogate() { Id = 2, Number = 3 }, "CA-3E-02-08-03-08-02");
        RoundTrip(RuntimeTypeModel.Default, new DerivedClassSurrogate { Id = 4, Number = 5 }, "CA-3E-02-08-05-08-04");
    }

    private static T RoundTrip<T>(TypeModel model, T value, string expected)
    {
        var ms = new MemoryStream();
        model.Serialize(ms, value);
        ms.Position = 0;
        if (!ms.TryGetBuffer(out var segment)) segment = new(ms.ToArray());
        var actual = BitConverter.ToString(segment.Array!, segment.Offset, segment.Count);
        Assert.Equal(expected, actual);
        return (T)model.Deserialize(ms, null, typeof(T));
    }

    public enum Mode
    {
        Runtime,
        CompileInPlace,
        CompileInMemory,
        CompileToFile,
    }

    [Theory]
    [InlineData(Mode.Runtime)]
    [InlineData(Mode.CompileInPlace)]
    [InlineData(Mode.CompileInMemory)]
#if (NETFX || NET9_0_OR_GREATER) && !(NET && BACK_COMPAT)
    [InlineData(Mode.CompileToFile)]
#endif
    public void RecursiveModel(Mode mode)
    {
        var model = RuntimeTypeModel.Create();
        model.AutoCompile = false;
        model.Add(typeof(RecursiveBase), true);
        model.Add(typeof(RecursiveConcrete), true);

        model.Add(typeof(RecursiveSurrogateBase), true);
        model.Add(typeof(RecursiveSurrogateConcrete), true);

        model.Add(typeof(RecursiveProperties), true);
        model.Add(typeof(RecursivePropertiesSurrogate), true);

        RecursiveBase parent = new RecursiveConcrete(12, 42),
            child = new RecursiveConcrete(15, 23);
        parent.Properties.Objects.Add(child);
        Verify(parent);

        object clone;
        switch (mode)
        {
            case Mode.Runtime:
                clone = model.DeepClone(parent);
                break;
            case Mode.CompileInPlace:
                model.CompileInPlace();
                clone = model.DeepClone(parent);
                break;
            case Mode.CompileInMemory:
                clone = model.Compile().DeepClone(parent);
                break;
            case Mode.CompileToFile:
                var dll = model.Compile(new RuntimeTypeModel.CompilerOptions
                {
                    TypeName = "RecursiveModel",
#if (NETFX || NET9_0_OR_GREATER) && !(NET && BACK_COMPAT)
                    OutputPath = "RecursiveModel.dll",
#endif
                });
#if !BACK_COMPAT
                PEVerify.AssertValid("RecursiveModel.dll");
#endif
                clone = dll.DeepClone(parent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        Verify(Assert.IsType<RecursiveConcrete>(clone));


        static void Verify(RecursiveBase root)
        {
            var parent = Assert.IsType<RecursiveConcrete>(root);
            Assert.Equal(12, parent.BaseId);
            Assert.Equal(42, parent.SubId);
            var child = Assert.IsType<RecursiveConcrete>(Assert.Single(parent.Properties.Objects));
            Assert.Equal(15, child.BaseId);
            Assert.Equal(23, child.SubId);
            Assert.Empty(child.Properties.Objects);
        }
    }

    [ProtoContract(Surrogate = typeof(RecursiveSurrogateBase))]
    public abstract class RecursiveBase(int baseId)
    {
        public int BaseId { get; } = baseId;
        internal abstract RecursiveSurrogateBase ConvertToSurrogate();

        public RecursiveProperties Properties { get; } = new();
    }

    [ProtoContract(Surrogate = typeof(RecursiveSurrogateConcrete))]
    public sealed class RecursiveConcrete(int baseId, int subId) : RecursiveBase(baseId)
    {
        public int SubId { get; } = subId;

        internal override RecursiveSurrogateBase ConvertToSurrogate()
            => new RecursiveSurrogateConcrete
            {
                BaseId = BaseId,
                SubId = SubId,
                Properties = Properties,
            };
    }

    [ProtoContract]
    [ProtoInclude(10, typeof(RecursiveSurrogateConcrete))]
    public abstract class RecursiveSurrogateBase
    {
        [ProtoMember(1)] public int BaseId { get; set; }

        [ProtoMember(2)] public RecursiveProperties Properties { get; set; }

        protected abstract RecursiveBase ConvertToObject();

        public static implicit operator RecursiveBase(RecursiveSurrogateBase surrogateBase)
            => surrogateBase?.ConvertToObject();

        public static implicit operator RecursiveSurrogateBase(RecursiveBase surrogate)
            => surrogate?.ConvertToSurrogate();
    }

    [ProtoContract]
    public sealed class RecursiveSurrogateConcrete : RecursiveSurrogateBase
    {
        [ProtoMember(1)] public int SubId { get; set; }

        protected override RecursiveBase ConvertToObject()
        {
            var obj = new RecursiveConcrete(BaseId, SubId);
            Utils.Fill(Properties, obj.Properties);
            return obj;
        }

        public static implicit operator RecursiveConcrete(RecursiveSurrogateConcrete surrogateBase)
            => (RecursiveConcrete)surrogateBase?.ConvertToObject();

        public static implicit operator RecursiveSurrogateConcrete(RecursiveConcrete surrogate)
            => (RecursiveSurrogateConcrete)surrogate?.ConvertToSurrogate();
    }

    [ProtoContract(Surrogate = typeof(RecursivePropertiesSurrogate))]
    public sealed class RecursiveProperties
    {
        public List<RecursiveBase> Objects { get; } = new();
    }

    private static class Utils
    {
        public static void Fill(RecursiveProperties from, RecursiveProperties to)
            => Fill(from.Objects, to.Objects);

        public static void Fill(List<RecursiveBase> from, List<RecursiveBase> to)
        {
#if NET8_0_OR_GREATER
            CollectionsMarshal.SetCount(to, from.Count);
            from.CopyTo(CollectionsMarshal.AsSpan(to));
#else
            to.AddRange(from);
#endif
        }
    }

    [ProtoContract]
    public sealed class RecursivePropertiesSurrogate
    {
        [ProtoMember(1)] public List<RecursiveBase> Objects { get; } = new();

        public static implicit operator RecursiveProperties(RecursivePropertiesSurrogate source)
        {
            if (source is null) return null;
            var obj = new RecursiveProperties();
            Utils.Fill(source.Objects, obj.Objects);
            return obj;
        }

        public static implicit operator RecursivePropertiesSurrogate(RecursiveProperties source)
        {
            if (source is null) return null;
            var obj = new RecursivePropertiesSurrogate();
            Utils.Fill(source.Objects, obj.Objects);
            return obj;
        }
    }
}