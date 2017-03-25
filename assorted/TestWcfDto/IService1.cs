using System.ComponentModel;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace TestWcfDto
{
    // NOTE: If you change the interface name "IService1" here, you must also update the reference to "IService1" in Web.config.
    [ServiceContract]
    public interface IService1
    {

        [OperationContract]
        string GetData(int value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        // TODO: Add your service operations here
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        [DataMember(Order=1)]
        public bool BoolValue {get;set;}
        [DataMember(Order = 2)]
        public string StringValue {get;set;}
    }
}
