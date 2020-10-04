# Using protobuf-net with Noda Time

[Noda Time](https://nodatime.org/) is "an alternative date and time API for .NET". protobuf-net has always had support for `DateTime` and `TimeSpan` (including support
for the well-known protobuf types, `Timestamp` and `Duration` via [`CompatibilityLevel`](http://protobuf-net.github.io/protobuf-net/compatibilitylevel)), but protobuf-net
now offers optional support for Noda Time types.

For example, say we have a type that we want to serialize, which makes use of `NodaTime.Instant` to represent a point in time (broadly similar to `DateTime`), or a `NodaTime.Duration`
(broadly similar to `TimeSpan`); we can include this in a protobuf-net model by:

- adding a package reference to [protobuf-net.NodaTime](https://www.nuget.org/packages/protobuf-net.NodaTime/)
- calling `RuntimeTypeModel.Default.AddNodaTime();` as part of application startup
- annotate the member with protobuf-net attributes as usual, for example:

    ``` c#
    [ProtoContract]
    public class MyType {
        // ...

        [ProtoMember(42)]
        public Instant ActivationTime {get;set;}

        // ...
    }
    ```

and: that's it. The library will now handle any of:

- `NodaTime.Duration`
- `NodaTime.Instant`
- `NodaTime.LocalDate`
- `NodaTime.LocalTime`
- `NodaTime.IsoDayOfWeek`

This is the exact same types that are supported by [NodaTime.Serialization.Protobuf](https://www.nuget.org/packages/NodaTime.Serialization.Protobuf) (which is used with
the [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf/) library), and the two implementations are byte-compatible for simple data exchange.

Additionally, any usage of `GetSchema()`/`GetProto<T>()` will give appropriate output indicating the native protobuf types being represented.

---

Note: [protobuf-net.NodaTime](https://www.nuget.org/packages/protobuf-net.NodaTime/) makes use of new extension APIs in protobuf-net v3, and therefore will not work
with earlier versions of protobuf-net.