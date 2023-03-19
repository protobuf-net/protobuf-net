# <img src="https://protogen.marcgravell.com/images/protobuf-net.svg" alt="protobuf-net logo" width="45" height="45"> protobuf-net
protobuf-net is a contract based serializer for .NET code, that happens to write data in the "protocol buffers" serialization format engineered by Google. The API, however, is very different to Google's, and follows typical .NET patterns (it is broadly comparable, in usage, to `XmlSerializer`, `DataContractSerializer`, etc). It should work for most .NET languages that write standard types and can use attributes.

[![Build status](https://ci.appveyor.com/api/projects/status/1pj6gk7h37bjn200/branch/main?svg=true)](https://ci.appveyor.com/project/StackExchange/protobuf-net/branch/main)

## Release Notes

[v3 is here!](https://protobuf-net.github.io/protobuf-net/3_0)

[Change history and pending changes are here](https://protobuf-net.github.io/protobuf-net/releasenotes).

---

## Supported Runtimes
- .NET 6.0+ (.NET 5 etc will use .NET Standard 2.1)
- .NET Standard 2.0, 2.1
- .NET Framework 4.6.2+

## Build tools

Build tools to help you use protobuf-net correctly are [available via `protobuf-net.BuildTools`](https://protobuf-net.github.io/protobuf-net/build_tools)

## Runtime Installation

All stable and some pre-release packages are available on NuGet. CI Builds are available via MyGet (feed URL: `https://www.myget.org/F/protobuf-net/api/v3/index.json `).

You can use the following command in the Package Manager Console:
```ps
Install-Package protobuf-net
```

| Package | NuGet Stable | NuGet Pre-release | Downloads | MyGet |
| ------- | ------------ | ----------------- | --------- | ----- |
| [protobuf-net](https://www.nuget.org/packages/protobuf-net/) | [![protobuf-net](https://img.shields.io/nuget/v/protobuf-net.svg)](https://www.nuget.org/packages/protobuf-net/) | [![protobuf-net](https://img.shields.io/nuget/vpre/protobuf-net.svg)](https://www.nuget.org/packages/protobuf-net/) | [![protobuf-net](https://img.shields.io/nuget/dt/protobuf-net.svg)](https://www.nuget.org/packages/protobuf-net/) | [![protobuf-net MyGet](https://img.shields.io/myget/protobuf-net/vpre/protobuf-net.svg)](https://www.myget.org/feed/protobuf-net/package/nuget/protobuf-net) |

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

* they must be positive integers (for best portability, they should be `<= 536870911` and not in the range `19000-19999`)
* they must be unique within a single type but the same numbers can be re-used in sub-types if inheritance is enabled 
* the identifiers must not conflict with any inheritance identifiers (discussed later) 
* lower numbers take less space - don't start at 100,000,000 
* the identifier is important; you can change the member-name, or shift it between a property and a field, but changing the identifier changes the data 

## Advanced subjects

### Inheritance

Inheritance must be explicitly declared, in a similar way that it must for XmlSerializer and DataContractSerializer. This is done via [ProtoInclude(...)] on each type with known sub-types: 

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

In v2+, everything that can be done with attributes can also be configured at runtime via `RuntimeTypeModel`. The Serializer.* methods are basically just shortcuts to RuntimeTypeModel.Default.*, so to manipulate the behaviour of Serializer.*, you must configure RuntimeTypeModel.Default. 

## Support

I try to be responsive to [Stack Overflow questions in the `protobuf-net` tag](https://stackoverflow.com/questions/tagged/protobuf-net), [issues logged on GitHub](https://github.com/protobuf-net/protobuf-net), [email](mailto:marc.gravell@gmail.com), etc. I don't currently offer a paid support channel. If I've helped you, feel free to [buy me a coffee](https://buymeacoff.ee/marcgravell) or see the "Sponsor" link [at the top of the GitHub page](https://github.com/protobuf-net/protobuf-net).
