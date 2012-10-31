using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Examples.Issues
{
    [TestFixture]
    public class SO13162642
    {
        [Test]
        public void Execute()
        {
            using (var f = File.Create("Data.protobuf"))
            {
                Serializer.Serialize<IEnumerable<DTO>>(f, GenerateData(1000000));
            }

            using (var f = File.OpenRead("Data.protobuf"))
            {
                var dtos = Serializer.DeserializeItems<DTO>(f, ProtoBuf.PrefixStyle.Base128, 1);
                Console.WriteLine(dtos.Count());
            }
            Console.Read();
        }

        static IEnumerable<DTO> GenerateData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // reduce to 1100 to use much less memory
                var dto = new DTO { Data = new byte[1101] };
                for (int j = 0; j < dto.Data.Length; j++)
                {
                    // fill with data
                    dto.Data[j] = (byte)(i + j);
                }
                yield return dto;
            }
        }

        [ProtoContract]
        class DTO
        {
            [ProtoMember(1, DataFormat = ProtoBuf.DataFormat.Group)]
            public byte[] Data { get; set; }
        }
    }
}
