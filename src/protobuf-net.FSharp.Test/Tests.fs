module ProtoBuf.FSharp.Test.Tests

open System
open Xunit
open ProtoBuf
open ProtoBuf.Meta
open System.IO
open ProtoBuf.FSharp

[<Fact>]
let ``Test Map`` () =
    let model = 
        (RuntimeTypeModel.Create ("fsharp"))
            .AddSerializer (typeof<Map<_,_>>, typeof<FSharpMapFactory>)

    let source = 
        ["a", 1; "b", 2; "c", 3]
        |> Map.ofList

    let source2 = 
        ["a", 1.; "b", 2.; "c", 3.]
        |> Map.ofList


    use ms = new MemoryStream (1024)

    model.Serialize(ms, source)
    ms.Position <- 0L

    let dest = model.Deserialize<Map<string,int>>(ms)

    Assert.True(source.Equals(dest))
    ms.Position <- 0L

    model.Serialize(ms, source2)
    ms.Position <- 0L

    let dest2 = model.Deserialize<Map<string,double>>(ms)

    Assert.True(source2.Equals(dest2))

[<Fact>]
let ``Test List`` () =
    let model = 
        (RuntimeTypeModel.Create ("fsharp"))
            .AddSerializer (typeof<_ list>, typeof<FSharpListFactory>)

    let source = 
        ["a", 1; "b", 2; "c", 3]

    use ms = new MemoryStream (1024)

    model.Serialize(ms, source)
    ms.Position <- 0L

    let dest = model.Deserialize<(string * int) list>(ms)

    Assert.True(source.Equals(dest))

[<Fact>]
let ``Test Set`` () =
    let model = 
        (RuntimeTypeModel.Create ("fsharp"))
            .AddSerializer (typeof<Set<_>>, typeof<FSharpSetFactory>)

    let source = 
        ["a", 1; "b", 2; "c", 3]
        |> Set.ofList

    use ms = new MemoryStream (1024)

    model.Serialize(ms, source)
    ms.Position <- 0L

    let dest = model.Deserialize<Set<string * int>>(ms)

    Assert.True(source.Equals(dest))

[<Fact>]
let ``Test All`` () =
    let model = 
        (((RuntimeTypeModel.Create ("fsharp"))
            .AddSerializer (typeof<Map<_,_>>, typeof<FSharpMapFactory>))
                .AddSerializer (typeof<_ list>, typeof<FSharpListFactory>))
                    .AddSerializer (typeof<Set<_>>, typeof<FSharpSetFactory>)

    let source = ["a", 1; "b", 2; "c", 3]
    let map = Map.ofList source
    let set = Set.ofList source

    use ms = new MemoryStream (1024)

    model.Serialize(ms, source)
    ms.Position <- 0L
    let dest = model.Deserialize<(string * int) list>(ms)

    ms.Position <- 0L
    model.Serialize(ms, set)
    ms.Position <- 0L
    let setdest = model.Deserialize<Set<string * int>>(ms)

    ms.Position <- 0L
    model.Serialize(ms, map)
    ms.Position <- 0L
    let mapdest = model.Deserialize<Map<string, int>>(ms)

    Assert.True(source.Equals(dest))
    Assert.True(set.Equals(setdest))
    Assert.True(map.Equals(mapdest))

[<Fact>]
let ``Test complex`` () =
    let model = 
        (((RuntimeTypeModel.Create ("fsharp"))
            .AddSerializer (typeof<Map<_,_>>, typeof<FSharpMapFactory>))
                .AddSerializer (typeof<_ list>, typeof<FSharpListFactory>))
                    .AddSerializer (typeof<Set<_>>, typeof<FSharpSetFactory>)

    let source = ["a", 1; "b", 2; "c", 3]
    let set = Set.ofList source
    let map = 
        ["first", set; "second", set; "third", set]
        |> Map.ofSeq

    use ms = new MemoryStream(1024)

    model.Serialize(ms, map)
    ms.Position <- 0L
    let dest = model.Deserialize<(Map<string, Set<string * int>>)>(ms)

    Assert.True(map.Equals(dest))

[<Fact>]
let ``Test more complex`` () =
    let model = 
        (((RuntimeTypeModel.Create ("fsharp"))
            .AddSerializer (typeof<Map<_,_>>, typeof<FSharpMapFactory>))
                .AddSerializer (typeof<_ list>, typeof<FSharpListFactory>))
                    .AddSerializer (typeof<Set<_>>, typeof<FSharpSetFactory>)

    let source = ["a", 1; "b", 2; "c", 3]
    let set = Set.ofList source
    let map = 
        ["first", set; "second", set; "third", set]
        |> Map.ofSeq
    let map2 = 
        ["Alpha", map; "Beta", map; "Kappa", map]
        |> Map.ofSeq

    use ms = new MemoryStream(10240)

    model.Serialize(ms, map2)
    ms.Position <- 0L
    let dest = model.Deserialize<(Map<string,Map<string, Set<string * int>>>)>(ms)

    Assert.True(map2.Equals(dest))