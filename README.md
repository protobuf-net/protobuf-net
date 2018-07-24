# protobuf-net
protobuf-net is a contract based serializer for .NET code, that happens to write data in the "protocol buffers" serialization format engineered by Google. The API, however, is very different to Google's, and follows typical .NET patterns (it is broadly comparable, in usage, to XmlSerializer, DataContractSerializer, etc). It should work for most .NET languages that write standard types and can use attributes.

## Release Notes

[Change history and pending changes are here](https://mgravell.github.io/protobuf-net/releasenotes).

To understand how protobuf-net relates to protobuf [see here](https://mgravell.github.io/protobuf-net/version).

---

## Supported Runtimes
- .NET Framework 2.0+
- .NET Standard 1.0+ (note that 1.0 is very restricted, and suits iOS etc; use the highest .NET Standard that works for your platform)
- UAP 10.0(+?)

It is possible to build for more specific TFMs, but *right now* I've simplified the build to those. If you need help
with a custom build: let me know.

## Runtime Installation

Packages are available on NuGet: [`protobuf-net`](https://www.nuget.org/packages/protobuf-net). You can use the following command in the Package Manager Console:
`Install-Package protobuf-net`

## Basic usage

### 1 First Decorate your classes
```csharp
[ProtoContract]
class Person {
    [ProtoMember(1)]
    public int Id {get;set;}
    [ProtoMember(2)]
    public string Name {get;set;}
    [ProtoMember(3)]
    public Address Address {get;set;}
}
[ProtoContract]
class Address {
    [ProtoMember(1)]
    public string Line1 {get;set;}
    [ProtoMember(2)]
    public string Line2 {get;set;}
}
```
Note that unlike XmlSerializer, the member-names are not encoded in the data - instead, you must pick an integer to identify each member. Additionally, to show intent it is necessary to show that we intend this type to be serialized (i.e. that it is a data contract).

### 2 Serialize your data

This writes a 32 byte file to "person.bin" :
```csharp
var person = new Person {
    Id = 12345, Name = "Fred",
    Address = new Address {
        Line1 = "Flat 1",
        Line2 = "The Meadows"
    }
};
using (var file = File.Create("person.bin")) {
    Serializer.Serialize(file, person);
}
```

### 3 Deserialize your data

This reads the data back from "person.bin" :
```csharp
Person newPerson;
using (var file = File.OpenRead("person.bin")) {
    newPerson = Serializer.Deserialize<Person>(file);
}
```

### Notes 

#### Notes for Identifiers

* they must be positive integers 
* they must be unique within a single type but the same numbers can be re-used in sub-types if inheritance is enabled 
* the identifiers must not conflict with any inheritance identifiers (discussed later) 
* lower numbers take less space - don't start 100,000,000 
* the identifier is important; you can change the member-name, or shift it between a property and a field, but changing the identifier changes the data 

#### Notes on types

supported: 
* custom classes that: 
  * are marked as data-contract 
  * have a parameterless constructor 
  * for Silverlight: are public 
* many common primitives etc 
* single dimension arrays: T[] 
* List<T> / IList<T> 
* Dictionary<TKey,TValue> / IDictionary<TKey,TValue> 
* any type which implements IEnumerable<T> and has an Add(T) method 

The code assumes that types will be mutable around the elected members. Accordingly, custom structs are not supported, since they should be immutable. 

## Advanced subjects

### Inheritance

Inheritance must be explicitly declared, in a similar way that if must for XmlSerializer and DataContractSerializer. This is done via [ProtoInclude(...)] on each type with known sub-types: 

```csharp
[ProtoContract]
[ProtoInclude(7, typeof(SomeDerivedType))]
class SomeBaseType {...}

[ProtoContract]
class SomeDerivedType {...}
```
There is no special significance in the 7 above; it is an integer key, just like every [ProtoMember(...)]. It must be unique in terms of SomeBaseType (no other [ProtoInclude(...)] or [ProtoMember(...)] in SomeBaseType can use 7), but does not need to be unique globally. 

### .proto file

As an alternative to writing your classes and decorating them, You can generate your types from a .proto schema using [`protogen`](https://protogen.marcgravell.com/);
the `protogen` tool is available as a zip from that location, or [as a "global tool"](https://www.nuget.org/packages/protobuf-net.Protogen) (multi-platform).

### Alternative to attributes

In v2, everything that can be done with attributes can also be configured at runtime via `RuntimeTypeModel`. The Serializer.* methods are basically just shortcuts to RuntimeTypeModel.Default.*, so to manipulate the behaviour of Serializer.*, you must configure RuntimeTypeModel.Default. 

## Support

I try to be responsive to [Stack Overflow questions in the `protobuf-net` tag](https://stackoverflow.com/questions/tagged/protobuf-net), [issues logged on github](https://github.com/mgravell/protobuf-net), [email](mailto:marc.gravell@gmail.com), etc. I don't currently offer a paid support channel. If I've helped you, feel free to [buy me a coffee](https://buymeacoff.ee/marcgravell).