using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

static class Program
{
    static void Main()
    {
        var model = new HockeyModel();
        
        var payload = new Payload();
        var wrapped = new Wrapper { Data = payload };
        var rand = new Random(12345);
        bool Add(int count)
        {
            var data = payload.Data;
            while (count > 0)
            {
                var len = rand.Next(50);
                long[] arr = new long[len];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = rand.Next();
                }
                data.Add(data.Count, arr);
                count--;
            }
            return count == 0;
        }


        Add(7500);

        Console.WriteLine("Count,Bytes,Milliseconds");
        for (int i = 0; i < 15 && Add(100); i++)
        {
            using (var ms = new MemoryStream())
            {
                var sw = Stopwatch.StartNew();
                model.Serialize(ms, wrapped);
                sw.Stop();
                Console.WriteLine($"{payload.Data.Count}, {ms.Length} bytes,{sw.ElapsedMilliseconds}");
            }
        }


        Console.WriteLine("All done");
    }
}

[ProtoContract]
public sealed class Payload
{
    [ProtoMember(3)]
    public Dictionary<long, long[]> Data { get; } = new Dictionary<long, long[]>();
}

[ProtoContract]
public sealed class Wrapper
{
    [ProtoMember(1)]
    public Payload Data { get; set; }
}

class HockeyModel : TypeModel
{
    protected override ISerializer<T> GetSerializer<T>() => SerializerCache.Get<MyServices, T>();
    sealed class MyServices : ISerializer<Payload>, ISerializer<Wrapper>, ISerializerProxy<long[]>
    {
        public ISerializer<long[]> Serializer => RepeatedSerializer.CreateVector<long>();

        SerializerFeatures ISerializer<Payload>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
        SerializerFeatures ISerializer<Wrapper>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

        Payload ISerializer<Payload>.Read(ref ProtoReader.State state, Payload value) => throw new NotImplementedException();

        Wrapper ISerializer<Wrapper>.Read(ref ProtoReader.State state, Wrapper value) => throw new NotImplementedException();

        void ISerializer<Payload>.Write(ref ProtoWriter.State state, Payload value)
        {
            Dictionary<long, long[]> data = value.Data;
            if (data != null)
            {
                Dictionary<long, long[]> values = data;
                MapSerializer.CreateDictionary<long, long[]>().WriteMap(ref state, 3, SerializerFeatures.OptionFailOnDuplicateKey | SerializerFeatures.WireTypeString, values, SerializerFeatures.WireTypeSpecified, SerializerFeatures.WireTypeSpecified, null, null);
            }
        }

        void ISerializer<Wrapper>.Write(ref ProtoWriter.State state, Wrapper value)
        {
            Payload data = value.Data;
            state.WriteMessage<Payload>(1, SerializerFeatures.CategoryRepeated, data, this);
        }
    }
}
