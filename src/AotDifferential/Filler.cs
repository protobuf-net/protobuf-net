using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace ProtoBuf.AotDifferential;

/// <summary>
/// Builds a populated instance of a contract by reflection, so the corpus can be compared on
/// <em>bytes</em> rather than only on whether it compiles.
/// </summary>
/// <remarks>
/// Values are deterministic and every scalar differs from the last, which is what makes a swapped
/// field number visible — two members that happen to hold the same value would serialize identically
/// under either numbering. Nothing here needs to be *correct* protobuf, only identical for both
/// models: the same object instance is handed to each.
/// </remarks>
internal sealed class Filler
{
    private const int MaxDepth = 4;
    private int _counter;

    /// <summary>Why a type could not be built, for the report; null when it could.</summary>
    public string LastFailure { get; private set; }

    public object Create(Type type)
    {
        _counter = 0;
        LastFailure = null;
        try
        {
            return Build(type, 0, []);
        }
        catch (Exception ex)
        {
            LastFailure = ex.GetType().Name + ": " + ex.Message;
            return null;
        }
    }

    private object Build(Type type, int depth, HashSet<Type> path)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null) return Build(underlying, depth, path);

        if (Scalar(type) is { } scalar) return scalar;

        // a cycle, or too deep: null for a reference, default for a value. Both models see the same
        // thing, so this costs coverage rather than correctness
        if (depth >= MaxDepth || !path.Add(type))
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
        try
        {
            if (type.IsArray) return BuildArray(type, depth, path);
            if (Dictionary(type) is { } dictionary) return BuildDictionary(type, dictionary, depth, path);
            if (Enumerable(type) is { } element) return BuildCollection(type, element, depth, path);

            // an abstract or interface member has to be given one of its declared sub-types
            var concrete = type.IsAbstract || type.IsInterface ? FirstSubType(type) : type;
            if (concrete is null) return null;

            var instance = Construct(concrete, depth, path);
            if (instance is not null) Populate(concrete, instance, depth, path);
            return instance;
        }
        finally
        {
            path.Remove(type);
        }
    }

    /// <summary>A deterministic value for anything protobuf-net treats as a scalar.</summary>
    private object Scalar(Type type)
    {
        var n = _counter;
        if (type.IsEnum)
        {
            _counter++;
            var values = Enum.GetValues(type);
            // a defined value rather than an arbitrary cast: an undefined one is legal on the wire
            // but makes a failure harder to read
            return values.Length == 0 ? Activator.CreateInstance(type) : values.GetValue(n % values.Length);
        }
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean: _counter++; return (n % 2) == 0;
            case TypeCode.SByte: _counter++; return (sbyte)(n % 100 + 1);
            case TypeCode.Byte: _counter++; return (byte)(n % 100 + 1);
            case TypeCode.Int16: _counter++; return (short)(n + 1);
            case TypeCode.UInt16: _counter++; return (ushort)(n + 1);
            case TypeCode.Int32: _counter++; return n + 1;
            case TypeCode.UInt32: _counter++; return (uint)(n + 1);
            case TypeCode.Int64: _counter++; return (long)(n + 1);
            case TypeCode.UInt64: _counter++; return (ulong)(n + 1);
            case TypeCode.Single: _counter++; return n + 1.5f;
            case TypeCode.Double: _counter++; return n + 1.5d;
            case TypeCode.Decimal: _counter++; return n + 1.5m;
            case TypeCode.Char: _counter++; return (char)('a' + (n % 26));
            case TypeCode.String: _counter++; return "s" + n;
            case TypeCode.DateTime:
                _counter++;
                // UTC and whole seconds: the level-200 form round-trips those exactly, and Kind is
                // not on the wire below level 240
                return new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(n);
        }
        if (type == typeof(TimeSpan)) { _counter++; return TimeSpan.FromSeconds(n + 1); }
        if (type == typeof(Guid))
        {
            _counter++;
            var bytes = new byte[16];
            bytes[0] = (byte)(n + 1);
            return new Guid(bytes);
        }
        if (type == typeof(Uri)) { _counter++; return new Uri("http://example.com/" + n); }
        if (type == typeof(byte[])) { _counter++; return new byte[] { (byte)(n + 1), (byte)(n + 2) }; }
        if (type == typeof(DateOnly)) { _counter++; return new DateOnly(2020, 1, 1).AddDays(n); }
        if (type == typeof(TimeOnly)) { _counter++; return new TimeOnly(0, 0).AddMinutes(n); }
        if (type == typeof(IntPtr)) { _counter++; return (IntPtr)(n + 1); }
        if (type == typeof(UIntPtr)) { _counter++; return (UIntPtr)(n + 1); }
        return null;
    }

    private object BuildArray(Type type, int depth, HashSet<Type> path)
    {
        var element = type.GetElementType();
        if (type.GetArrayRank() != 1) return null;
        var array = Array.CreateInstance(element, 2);
        for (int i = 0; i < 2; i++)
        {
            var value = Build(element, depth + 1, path);
            // protobuf-net rejects null elements (ThrowNullRepeatedContents), so an unbuildable
            // element means an empty collection rather than one with a hole in it
            if (value is null) return Array.CreateInstance(element, 0);
            array.SetValue(value, i);
        }
        return array;
    }

    private object BuildCollection(Type type, Type element, int depth, HashSet<Type> path)
    {
        var concrete = ConcreteCollection(type, element);
        if (concrete is null) return null;
        object instance;
        try { instance = Activator.CreateInstance(concrete); }
        catch { return null; }

        var add = concrete.GetMethod("Add", [element])
            ?? concrete.GetMethod("Enqueue", [element])
            ?? concrete.GetMethod("Push", [element]);
        if (add is null) return instance;

        for (int i = 0; i < 2; i++)
        {
            var value = Build(element, depth + 1, path);
            if (value is null) break;
            try { add.Invoke(instance, [value]); }
            catch { break; }
        }
        return instance;
    }

    private object BuildDictionary(Type type, (Type Key, Type Value) pair, int depth, HashSet<Type> path)
    {
        var concrete = type.IsInterface || type.IsAbstract
            ? typeof(Dictionary<,>).MakeGenericType(pair.Key, pair.Value) : type;
        object instance;
        try { instance = Activator.CreateInstance(concrete); }
        catch { return null; }

        var add = concrete.GetMethod("Add", [pair.Key, pair.Value]);
        if (add is null) return instance;
        for (int i = 0; i < 2; i++)
        {
            var key = Build(pair.Key, depth + 1, path);
            var value = Build(pair.Value, depth + 1, path);
            if (key is null || value is null) break;
            try { add.Invoke(instance, [key, value]); }
            catch { break; } // a duplicate key, most likely
        }
        return instance;
    }

    private object Construct(Type type, int depth, HashSet<Type> path)
    {
        if (type.IsValueType) return Activator.CreateInstance(type);

        var parameterless = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);
        if (parameterless is not null)
        {
            try { return parameterless.Invoke(null); }
            catch { /* fall through */ }
        }

        // an auto-tuple, or anything else that only constructs with arguments
        foreach (var ctor in type.GetConstructors().OrderBy(static x => x.GetParameters().Length))
        {
            var parameters = ctor.GetParameters();
            var arguments = new object[parameters.Length];
            var ok = true;
            for (int i = 0; i < parameters.Length && ok; i++)
            {
                arguments[i] = Build(parameters[i].ParameterType, depth + 1, path);
                ok = arguments[i] is not null || !parameters[i].ParameterType.IsValueType;
            }
            if (!ok) continue;
            try { return ctor.Invoke(arguments); }
            catch { /* try the next */ }
        }

        // [ProtoContract(SkipConstructor = true)] types need no constructor at all, and protobuf-net
        // would use exactly this to make one
        try { return RuntimeHelpers.GetUninitializedObject(type); }
        catch { return null; }
    }

    private void Populate(Type type, object instance, int depth, HashSet<Type> path)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
            var value = Build(property.PropertyType, depth + 1, path);
            if (value is null) continue;

            if (property.CanWrite)
            {
                try { property.SetValue(instance, value); continue; }
                catch { continue; }
            }
            // a getter-only property is reached through its backing field, which is the same route
            // the generator takes - and the only way these members get a value to compare at all
            var backing = type.GetField($"<{property.Name}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (backing is null) continue;
            try { backing.SetValue(instance, value); } catch { }
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.IsInitOnly || field.IsLiteral) continue;
            var value = Build(field.FieldType, depth + 1, path);
            if (value is null) continue;
            try { field.SetValue(instance, value); } catch { }
        }

        // {Name}Specified is matched by name and *replaces* the trivial-value guard, so leaving it
        // false suppresses the member on both sides - true is the setting that exercises anything
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.Name.EndsWith("Specified", StringComparison.Ordinal)) continue;
            if (property.PropertyType != typeof(bool) || !property.CanWrite) continue;
            try { property.SetValue(instance, true); } catch { }
        }
    }

    /// <summary>The first <c>[ProtoInclude]</c> sub-type of an abstract or interface member.</summary>
    private static Type FirstSubType(Type type)
    {
        foreach (var attribute in type.GetCustomAttributesData())
        {
            if (attribute.AttributeType.FullName != "ProtoBuf.ProtoIncludeAttribute") continue;
            foreach (var argument in attribute.ConstructorArguments)
            {
                if (argument.Value is Type known && !known.IsAbstract && !known.IsInterface) return known;
                // the string overload resolves at runtime; not our problem here
                if (argument.Value is string name && Type.GetType(name, throwOnError: false) is { } byName)
                {
                    return byName;
                }
            }
        }
        return null;
    }

    private static Type ConcreteCollection(Type type, Type element)
    {
        if (!type.IsInterface && !type.IsAbstract) return type;
        var list = typeof(List<>).MakeGenericType(element);
        return type.IsAssignableFrom(list) ? list : null;
    }

    private static (Type Key, Type Value)? Dictionary(Type type)
    {
        foreach (var candidate in Interfaces(type))
        {
            if (candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var arguments = candidate.GetGenericArguments();
                return (arguments[0], arguments[1]);
            }
        }
        return null;
    }

    private static Type Enumerable(Type type)
    {
        if (type == typeof(string)) return null;
        foreach (var candidate in Interfaces(type))
        {
            if (candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return candidate.GetGenericArguments()[0];
            }
        }
        return typeof(IEnumerable).IsAssignableFrom(type) ? typeof(object) : null;
    }

    private static IEnumerable<Type> Interfaces(Type type)
        => type.IsInterface ? new[] { type }.Concat(type.GetInterfaces()) : type.GetInterfaces();
}
