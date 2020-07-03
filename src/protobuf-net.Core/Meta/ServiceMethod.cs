using System;
namespace ProtoBuf.Meta
{
    /// <summary>
    /// Describes a method of a service.
    /// </summary>
    public sealed class ServiceMethod
    {
        /// <summary>
        /// The name of the method.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type sent by the client.
        /// </summary>
        public Type InputType { get; }

        /// <summary>
        /// The type returned from the server.
        /// </summary>
        public Type OutputType { get; }

        /// <summary>
        /// The shape of the API in terms of the arity of the input/output vales.
        /// </summary>
        public MethodType MethodType { get; }

        internal bool ServerStreaming => MethodType switch {
            MethodType.ServerStreaming => true,
            MethodType.DuplexStreaming => true,
            _ => false
        };
        internal bool ClientStreaming => MethodType switch
        {
            MethodType.ClientStreaming => true,
            MethodType.DuplexStreaming => true,
            _ => false
        };

        /// <summary>
        /// Create a mew <see cref="ServiceMethod"/> instance.
        /// </summary>
        public ServiceMethod(string name, Type inputType, Type outputType, MethodType type)
        {
            Name = name;
            InputType = inputType;
            OutputType = outputType;
            MethodType = type;
        }
    }

    public enum MethodType
    {
        //
        // Summary:
        //     Single request sent from client, single response received from server.
        Unary = 0,
        //
        // Summary:
        //     Stream of request sent from client, single response received from server.
        ClientStreaming = 1,
        //
        // Summary:
        //     Single request sent from client, stream of responses received from server.
        ServerStreaming = 2,
        //
        // Summary:
        //     Both server and client can stream arbitrary number of requests and responses
        //     simultaneously.
        DuplexStreaming = 3
    }
}
