# protobuf-net
protobuf-net is a contract based serializer for .NET code, that happens to write data in the "protocol buffers" serialization format engineered by Google. The API, however, is very different to Google's, and follows typical .NET patterns (it is broadly comparable, in usage, to XmlSerializer, DataContractSerializer, etc). It should work for most .NET languages that write standard types and can use attributes.

## Release Notes

[Change history and pending changes are here](http://mgravell.github.io/protobuf-net/releasenotes)

## Donate

If you feel like supporting my efforts, I won't stop you:

<a href='https://pledgie.com/campaigns/33946'><img alt='Click here to lend your support to: protobuf-net; fast binary serialization for .NET and make a donation at pledgie.com !' src='https://pledgie.com/campaigns/33946.png?skin_name=chrome' border='0' ></a>

If you can't, that's fine too.

---

Supported Runtimes :
- .NET Framework 4.0+
- .NET Standard 1.3+

Legacy Runtimes (up to v2.1.0)
- .NET Framework 2.0/3.0/3.5
- Compact Framework 2.0/3.5
- Mono 2.x
- Silverlight, Windows Phone 7&8
- Windows 8 apps

## install

Nuget : `Install-Package protobuf-net`

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

As an alternative to writing your classes and decorating them, You can generate your types and serializer from a .proto schema. 

This done using the precompiler. [Additional guidance can be found here](http://blog.marcgravell.com/2012/07/introducing-protobuf-net-precompiler.html).

### Alternative to attributes

In v2, everything that can be done with attributes can also be configured at runtime via RuntimeTypeModel. The Serializer.* methods are basically just shortcuts to RuntimeTypeModel.Default.*, so to manipulate the behaviour of Serializer.*, you must configure RuntimeTypeModel.Default. 

