using System.IO;
using System.Net;
using ProtoBuf;

namespace HttpClient
{
    /// <summary>
    /// Utility methods to make WebClient easier to use with HTTP
    /// </summary>
    public static class ProtoWebClient
    {
        public static void UploadProto(this WebClient client, string address, object obj)
        {
            byte[] data;
            using (var ms = new MemoryStream()) {
                Serializer.NonGeneric.Serialize(ms, obj);
                data = ms.ToArray();
            }
            client.UploadData(address, data);
        }
        public static T UploadProto<T>(this WebClient client, string address, object obj)
        {
            byte[] data;
            using (var ms = new MemoryStream()) {
                Serializer.NonGeneric.Serialize(ms, obj);
                data = ms.ToArray();
            }
            data = client.UploadData(address, data); // data is now the response
            using (var ms = new MemoryStream(data)) {
                return Serializer.Deserialize<T>(ms);
            }
        }

        public static T DownloadProto<T>(this WebClient client, string address)
        {
            byte[] data = client.DownloadData(address);
            using (var ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}
