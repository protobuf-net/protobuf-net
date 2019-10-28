using ProtoBuf;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

static class Program
{
    static void Main()
    {
        var payload = new Payload();
        var wrapped = new Wrapper { Data = payload };
        var rand = new Random(12345);
        void Add(int count)
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
        }
        Add(7500);
        using (var ms = new MemoryStream())
        {
            ms.Position = 0;
            ms.SetLength(0);
            Console.WriteLine("Count,Bytes,Milliseconds");
            for (int i = 0; i < 150; i++)
            {
                Add(100);
                var state = ProtoWriter.State.Create(ms, null);
                try
                {
                    var sw = Stopwatch.StartNew();
                    MyServices.Write(ref state, wrapped);
                    sw.Stop();
                    Console.WriteLine($"{payload.Data.Count}, {ms.Length} bytes,{sw.ElapsedMilliseconds}");
                    state.Flush();
                }
                catch
                {
                    state.Abandon();
                    throw;
                }
                finally
                {
                    state.Dispose();
                }
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


static class MyServices
{
    static readonly RepeatedSerializer<long[],long> s_LongVector = RepeatedSerializer.CreateVector<long>();

    public static void Write(ref ProtoWriter.State state, Wrapper value)
    {
        Payload payload = value.Data;
        if (payload != null)
        {
            state.WriteFieldHeader(1, WireType.String);
            var tok = state.StartSubItem(payload);
            var data = payload.Data;
            if (data != null)
            {
                foreach (var pair in data)
                {
                    state.WriteFieldHeader(1, WireType.String);
                    var tok2 = state.StartSubItem(null);
                    state.WriteFieldHeader(1, WireType.Varint);
                    state.WriteInt64(pair.Key);
                    WritePacked(ref state, pair.Value);

                    state.EndSubItem(tok2);
                }
            }
            state.EndSubItem(tok);
        }
    }

    static void WritePacked(ref ProtoWriter.State state, long[] arr)
    {
        if (arr == null) return;
        switch(arr.Length)
        {
            case 0:
                state.WriteString(2, "");
                break;
            case 1:
                state.WriteFieldHeader(1, WireType.Varint);
                state.WriteInt64(arr[0]);
                break;
            default:
                state.WriteFieldHeader(2, WireType.String);
                var tok = state.StartSubItem(arr);
                state.SetPackedField(2);
                for (int i = 0; i < arr.Length; i++)
                {
                    state.WriteFieldHeader(2, WireType.Varint);
                    state.WriteInt64(arr[i]);
                }
                state.ClearPackedField(2);
                state.EndSubItem(tok);
                break;
        }
        //s_LongVector.WriteRepeated(ref state, 2, SerializerFeatures.WireTypeVarint | SerializerFeatures.WireTypeSpecified, arr, null);
    }
}