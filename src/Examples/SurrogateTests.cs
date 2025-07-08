using ProtoBuf.Meta;
using System.Runtime.Serialization;
using Xunit;

namespace ProtoBuf;

public class SurrogateTests
{
    public class BaseClass(int id)
    {
        public int Id => id;
    }
    public class DerivedClass(int id, int number) : BaseClass(id)
    {
        public int Number => number;
    }

    [ProtoContract]
    [ProtoInclude(1001, typeof(DerivedClassSurrogate))]
    public class BaseClassSurrogate
    {
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

        [ProtoMember(1)]
        public int Id { get; set; }

        [OnDeserializing]
        public virtual void BeforeDeserialize() { }
    }

    [ProtoContract]
    public class DerivedClassSurrogate : BaseClassSurrogate
    {
        [ProtoMember(1)]
        public int Number { get; set; }
    }

    [Fact]
    public void Execute()
    {

        RuntimeTypeModel model = RuntimeTypeModel.Create();

        model.Add(typeof(DerivedClass), false);
        model.Add(typeof(BaseClass), false)
            .AddSubType(1001, typeof(DerivedClass))
            .SetSurrogate(typeof(BaseClassSurrogate));
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

