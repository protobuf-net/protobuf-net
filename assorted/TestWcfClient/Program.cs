
using TestWcfClient.ServiceReference1;
using TestWcfDto;
namespace TestWcfClient
{
    class Program
    {
        static void Main()
        {
            using (var client = new Service1Client())
            {
                string s = client.GetData(123);
                var ct = new CompositeType { BoolValue = true, StringValue = s };
                var resp = client.GetDataUsingDataContract(ct);
            }
        }
    }
}
