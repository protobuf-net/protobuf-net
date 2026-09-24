# protobuf-net.FSharp

Serializers that teach [protobuf-net](https://www.nuget.org/packages/protobuf-net) about the F# collection
types — `list`, `Map` and `Set` — which otherwise have no usable .NET collection idiom to bind to.

Register the ones you need against a model:

```fsharp
let model =
    (RuntimeTypeModel.Create "fsharp")
        .AddSerializer(typeof<_ list>, typeof<FSharpListFactory>)
        .AddSerializer(typeof<Map<_,_>>, typeof<FSharpMapFactory>)
        .AddSerializer(typeof<Set<_>>, typeof<FSharpSetFactory>)
```

Everything else about protobuf-net is unchanged: contracts, field numbers and the wire format all behave as
they do from C#.

## More

- [Documentation](https://docs.protobuf-net.dev/)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
