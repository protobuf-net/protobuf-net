using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServer
{
    [ServiceContract]
    public interface INWindService
    {
        [OperationContract]
        OrderSet LoadFoo();

        [OperationContract]
        [ProtoBuf.ServiceModel.ProtoBehavior]
        OrderSet LoadBar();

        [OperationContract]
        OrderSet RoundTripFoo(OrderSet set);

        [OperationContract]
        [ProtoBuf.ServiceModel.ProtoBehavior]
        OrderSet RoundTripBar(OrderSet set);
    }

}
