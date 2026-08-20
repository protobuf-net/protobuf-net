# protobuf-net.HybridCache

A [`HybridCache`](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid) serializer backed by
[protobuf-net](https://www.nuget.org/packages/protobuf-net), so cached values are stored using your existing
protobuf contracts rather than JSON.

```csharp
builder.Services.AddHybridCache();
builder.Services.AddProtobufNetHybridCacheSerializer();
```

That handles every protobuf-net contract type. To use protobuf for one specific type only, or to supply the
`TypeModel` to serialize with:

```csharp
builder.Services.AddProtobufNetHybridCacheSerializer<MyType>();
builder.Services.AddProtobufNetHybridCachModel(MyCustomModel);
```

## More

- [Documentation](https://docs.protobuf-net.dev/)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
