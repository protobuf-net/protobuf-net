using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

static class Program
{
    static void Main()
    {
        Serializer.PrepareSerializer<Payload>();
        Serializer.PrepareSerializer<Wrapper>();

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


        Add(7000);

        for (int i = 0; i < 1000 && Add(100); i++)
        {
            using (var ms = new MemoryStream())
            {
                var sw = Stopwatch.StartNew();
                Serializer.Serialize(ms, payload);
                sw.Stop();
                Console.Write($"{payload.Data.Count}: {ms.Length} bytes/{sw.ElapsedMilliseconds}ms vs ");
            }
            using (var ms = new MemoryStream())
            {
                var sw = Stopwatch.StartNew();
                Serializer.Serialize(ms, wrapped);
                sw.Stop();
                Console.WriteLine($"{ms.Length} bytes/{sw.ElapsedMilliseconds}ms");
            }
        }


        Console.WriteLine("All done");
    }

    [ProtoContract]
    public class Payload
    {
        [ProtoMember(3)]
        public Dictionary<long, long[]> Data { get; } = new Dictionary<long, long[]>();
    }

    [ProtoContract]
    public class Wrapper
    {
        [ProtoMember(1)]
        public Payload Data { get; set; }
    }
}