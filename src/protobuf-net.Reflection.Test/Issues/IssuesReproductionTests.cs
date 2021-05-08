using Google.Protobuf.Reflection;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class IssuesReproductionTests
    {
        [Fact]
        public void CorrectlyParsesMessagesWithQuotationMarkInComments()
        {
            var set = new FileDescriptorSet();
            using var fileWithoutQuotationMarkInComments =
                File.OpenText(Path.Combine("Issues", "messagesWithoutQuotationMarkInComments.proto"));
            using var fileWithQuotationMarkInComments =
                File.OpenText(Path.Combine("Issues", "messagesWithQuotationMarkInComments.proto"));
            set.Add("messagesWithoutQuotationMarkInComments.proto", true, fileWithoutQuotationMarkInComments);
            set.Add("messagesWithQuotationMarkInComments.proto", true, fileWithQuotationMarkInComments);

            set.Process();

            Assert.Equal(set.Files[0].MessageTypes.Count, set.Files[1].MessageTypes.Count);
        }
    }
}