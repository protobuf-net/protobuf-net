using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServer
{
    // NOTE: If you change the interface name "IBasicService" here, you must also update the reference to "IBasicService" in Web.config.
    [ServiceContract]
    public interface IBasicService
    {
        [OperationContract]
        BasicType BasicOperation();
    }

    [DataContract]
    public class BasicType
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }        
    }
}
