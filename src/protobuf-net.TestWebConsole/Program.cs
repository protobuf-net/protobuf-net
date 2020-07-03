using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace protobuf_net.TestWebConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // expect "http://localhost:54921/" or similar for args[0]
            var uri = new Uri(new Uri(args.Single()), "/show/");
            Console.WriteLine($"Talking to {uri}...");

            var obj = new MyObject { Id = 143, Name = "abc" };
            Console.WriteLine($"Sending: {obj.Id}, '{obj.Name}'");
            var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            ms.Position = 0;
            var content = new StreamContent(ms);
            Console.WriteLine($"Payload is {ms.Length} bytes");
            content.Headers.ContentType =  new MediaTypeHeaderValue("application/protobuf");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));
            var response = await client.PostAsync(uri, content);
            Console.WriteLine($"Got: {response.StatusCode}, {response.Content.Headers.ContentType}, {response.Content.Headers.ContentLength} bytes");
            using var body = await response.Content.ReadAsStreamAsync();
            var clone = Serializer.Deserialize<MyObject>(body);
            Console.WriteLine($"Received: {clone.Id}, '{clone.Name}'");
        }
    }

    [ProtoContract]
    public class MyObject
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }
}
