using System;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// Marks a <c>partial class</c> as the container for build-time Connect proxies and bindings.
    /// </summary>
    /// <remarks>
    /// The consumer-written half of the arrangement, and the only thing they have to write beyond the
    /// contract itself:
    /// <code>
    /// [ProtoConnect(Model = typeof(MyModel))]
    /// [ProtoService(typeof(IGreeter), typeof(GreeterService))]
    /// internal partial class MyServices { }
    /// </code>
    /// Seeding uses protobuf-net.Grpc's <c>[ProtoService]</c> unchanged rather than a Connect-specific
    /// copy - it already says exactly what is needed, contract plus implementation, and a second
    /// spelling of it would be a second thing to keep in step. Only the container attribute differs,
    /// because it is what selects the transport.
    /// <para>
    /// The generator matches this by <b>full name</b>, not by symbol, in keeping with every other
    /// trigger attribute here - so test harnesses can declare their own and a reflectively-loaded
    /// generator needs no shared identity.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ProtoConnectAttribute : Attribute
    {
        /// <summary>
        /// The <c>[ProtoModel]</c>-generated model supplying serializers for the payload types.
        /// </summary>
        public Type? Model { get; set; }
    }
}
