#if FEAT_SERVICEMODEL && PLAT_XMLSERIALIZER
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// Behavior to swap out DatatContractSerilaizer with the XmlProtoSerializer for a given endpoint.
    ///  <example>
    /// Add the following to the server and client app.config in the system.serviceModel section:
    ///  <behaviors>
    ///    <endpointBehaviors>
    ///      <behavior name="ProtoBufBehaviorConfig">
    ///        <ProtoBufSerialization/>
    ///      </behavior>
    ///    </endpointBehaviors>
    ///  </behaviors>
    ///  <extensions>
    ///    <behaviorExtensions>
    ///      <add name="ProtoBufSerialization" type="ProtoBuf.ServiceModel.ProtoBehaviorExtension, protobuf-net, Version=1.0.0.255, Culture=neutral, PublicKeyToken=257b51d87d2e4d67"/>
    ///    </behaviorExtensions>
    ///  </extensions>
    /// 
    /// Configure your endpoints to have a behaviorConfiguration as follows:
    /// 
    ///  <service name="TK.Framework.Samples.ServiceModel.Contract.SampleService">
    ///    <endpoint address="http://myhost:9003/SampleService" binding="basicHttpBinding" behaviorConfiguration="ProtoBufBehaviorConfig"
    ///     bindingConfiguration="basicHttpBindingConfig" name="basicHttpProtoBuf" contract="ISampleServiceContract" />
    ///  </service>
    ///  <client>
    ///      <endpoint address="http://myhost:9003/SampleService" binding="basicHttpBinding"
    ///          bindingConfiguration="basicHttpBindingConfig" contract="ISampleServiceContract"
    ///          name="BasicHttpProtoBufEndpoint" behaviorConfiguration="ProtoBufBehaviorConfig"/>
    ///   </client>
    /// </example>
    /// </summary>
    public class ProtoEndpointBehavior : IEndpointBehavior
    {
        #region IEndpointBehavior Members

        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
            ReplaceDataContractSerializerOperationBehavior(endpoint, clientRuntime);
        }

        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            ReplaceDataContractSerializerOperationBehavior(endpoint, endpointDispatcher);
        }

        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {
        }

        #endregion

        private static void ReplaceDataContractSerializerOperationBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime clientRuntime)
        {
            foreach (OperationDescription description in serviceEndpoint.Contract.Operations)
                ReplaceDataContractSerializerOperationBehavior(description, clientRuntime);
        }

        private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description, ClientRuntime clientRuntime)
        {
            DataContractSerializerOperationBehavior dcsOperationBehavior = description.Behaviors.Find<DataContractSerializerOperationBehavior>();
            if (dcsOperationBehavior == null)
                return;

            ProtoOperationBehavior protoBehavior = new ProtoOperationBehavior(description);
            protoBehavior.MaxItemsInObjectGraph = dcsOperationBehavior.MaxItemsInObjectGraph;
            ((IOperationBehavior)protoBehavior).ApplyClientBehavior(description, clientRuntime.Operations[description.Name]);
        }

        private static void ReplaceDataContractSerializerOperationBehavior(ServiceEndpoint serviceEndpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            foreach (OperationDescription description in serviceEndpoint.Contract.Operations)
                ReplaceDataContractSerializerOperationBehavior(description, endpointDispatcher);
        }

        private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            DataContractSerializerOperationBehavior dcsOperationBehavior = description.Behaviors.Find<DataContractSerializerOperationBehavior>();
            if (dcsOperationBehavior == null)
                return;

            ProtoOperationBehavior protoBehavior = new ProtoOperationBehavior(description);
            protoBehavior.MaxItemsInObjectGraph = dcsOperationBehavior.MaxItemsInObjectGraph;
            ((IOperationBehavior)protoBehavior).ApplyDispatchBehavior(description, endpointDispatcher.DispatchRuntime.Operations[description.Name]);
        }

    }

}
#endif