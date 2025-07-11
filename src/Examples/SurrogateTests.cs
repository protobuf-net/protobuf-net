using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf;

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
        [ProtoMember(1)]
        public int Id { get; set; }

        [OnDeserializing]
        public virtual void BeforeDeserialize() { }
        
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
        [ProtoMember(1)]
        public int Number { get; set; }

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
    
    // not supported: setting surrogate at derived tier
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
    public void Execute(bool baseSurrogate, bool derivedSurrogate, TestMode mode)
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
                    scenario = model.Compile("MyModel", $"Surrogate_{key}.dll");
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
            
            var b = Assert.IsType<DerivedClass>(RoundTrip<BaseClass>(model, new DerivedClass(2, 3), "CA-3E-02-08-03-08-02"));
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
        RoundTrip(RuntimeTypeModel.Default, new BaseClassSurrogate { Id = 1}, "08-01");
        RoundTrip(RuntimeTypeModel.Default, new DerivedClassSurrogate() { Id = 2, Number = 3}, "CA-3E-02-08-03-08-02");
        RoundTrip(RuntimeTypeModel.Default, new DerivedClassSurrogate { Id = 4, Number = 5}, "CA-3E-02-08-05-08-04");
    }
    
    private static T RoundTrip<T>(TypeModel model, T value, string expected)
    {
        var ms = new MemoryStream();
        model.Serialize<T>(ms, value);
        ms.Position = 0;
        if (!ms.TryGetBuffer(out var segment)) segment = new(ms.ToArray());
        var actual = BitConverter.ToString(segment.Array!, segment.Offset, segment.Count);
        Assert.Equal(expected, actual);
        return model.Deserialize<T>(ms);
    }
}

/*
using ProtoBuf;
using ProtoBuf.Meta;

var model = RuntimeTypeModel.Create();
model.Add(typeof(Base2), true);
model.Add(typeof(Complex), true);
model.Add(typeof(ComplexSurrogate), true);

var a = model.Add(typeof(Base), false).AddSubType(2, typeof(Derived));
var b = model.Add(typeof(Derived), false);
a.SetSurrogate(typeof(BaseSurogate));
b.SetSurrogate(typeof(DerivedSurrogate));
model.CompileInPlace();

var obj = new Derived { Id = 5, Number = 6 };
var ms = new MemoryStream();
model.Serialize(ms, obj);
ms.Position = 0;
var clone = model.Deserialize(ms, null, typeof(Base));
Console.WriteLine("Y:" + clone); // Y:Derived:Id=5,Number=6

if (!ms.TryGetBuffer(out var segment)) segment = ms.ToArray();
Console.WriteLine(BitConverter.ToString(segment.Array!, segment.Offset, segment.Count)); // 1A-02-08-06-08-05


Base2 value = new Complex { Id = 5, Number = 6 };
ms = new MemoryStream();
model.Serialize(ms, value);
ms.Position = 0;
clone = model.Deserialize(ms, null, typeof(Base2));
Console.WriteLine("Z:" + clone); // Z:Complex:Id=5,Number=6

if (!ms.TryGetBuffer(out segment)) segment = ms.ToArray();
Console.WriteLine(BitConverter.ToString(segment.Array!, segment.Offset, segment.Count)); // 12-04-50-05-58-06-28-05

public class Base
{
    public override string ToString() => $"{GetType().Name}:Id={Id}";
    public int Id { get; set; }
}
public class Derived : Base
{
    public override string ToString() => $"{GetType().Name}:Id={Id},Number={Number}";
    public int Number { get; set; }
}

[ProtoContract]
[ProtoInclude(3, typeof(DerivedSurrogate))]
public class BaseSurogate
{
    public static implicit operator Base(BaseSurogate value)
    {
        Console.WriteLine($"AAA: {value?.GetType().Name}");
        return value switch
        {
            null => null!,
            DerivedSurrogate derived => new Derived() { Id = derived.Id, Number = derived.Number },
            _ => new Base() { Id = value.Id },
        };
    }
    public static implicit operator BaseSurogate(Base value)
    {
        Console.WriteLine($"BBB: {value?.GetType().Name}");
        return value switch
        {
            null => null!,
            Derived derived => new DerivedSurrogate() { Id = derived.Id, Number = derived.Number },
            _ => new BaseSurogate() { Id = value.Id },
        };
    }
    [ProtoMember(1)]
    public int Id { get; set; }
}


[ProtoContract]
public class DerivedSurrogate : BaseSurogate
{
    public static implicit operator Derived(DerivedSurrogate value)
    {
        Console.WriteLine($"CCC: {value?.GetType().Name}");
        return value is null ? null! : new() { Id = value.Id, Number = value.Number };
    }
    public static implicit operator DerivedSurrogate(Derived value)
    {
        Console.WriteLine($"DDD: {value?.GetType().Name}");
        return value is null ? null! : new() { Id = value.Id, Number = value.Number };
    }

    [ProtoMember(1)]
    public int Number { get; set; }
}


[ProtoContract]
[ProtoInclude(1, typeof(Simple))]
[ProtoInclude(2, typeof(Complex))]
public abstract class Base2
{
    [ProtoMember(5)]
    public int Id { get; set; }

    public override string ToString() => $"{GetType().Name}:Id={Id}";
}

[ProtoContract]
public class Simple : Base2
{

}

[ProtoContract(Surrogate = typeof(ComplexSurrogate))]
public class Complex : Base2
{
    public int Number { get; set; }

    public override string ToString() => $"{GetType().Name}:Id={Id},Number={Number}";
}

[ProtoContract(Name = nameof(Complex))]
public class ComplexSurrogate
{
    public override string ToString() => $"{GetType().Name}:Id={Id},Number={Number}";

    [ProtoMember(10)]
    public int Id { get; set; }

    [ProtoMember(11)]
    public int Number { get; set; }
    [ProtoConverter]
    public static ComplexSurrogate Convert(Complex source) => source is null ? null! : new ComplexSurrogate() { Id = source.Id, Number = source.Number };

    [ProtoConverter]
    public static Complex Convert(ComplexSurrogate source) => source is null ? null! : new Complex() { Id = source.Id, Number = source.Number };
}
*/

