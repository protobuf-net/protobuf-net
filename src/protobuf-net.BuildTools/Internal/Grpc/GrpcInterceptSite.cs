#nullable enable
using System;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>Which overload of <c>CreateGrpcService</c> a call site used.</summary>
    internal enum GrpcReceiverKind
    {
        /// <summary><c>this CallInvoker client</c> - the invoker is passed straight through.</summary>
        CallInvoker,

        /// <summary><c>this ChannelBase client</c> - needs <c>CreateCallInvoker()</c> first.</summary>
        ChannelBase,
    }

    /// <summary>
    /// One <c>channel.CreateGrpcService&lt;TService&gt;()</c> call site that can be pointed at a
    /// generated model instead of the reflective default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Holds the <c>[InterceptsLocation]</c> payload as the already-encoded <c>version</c>/<c>data</c>
    /// pair rather than anything Roslyn-shaped, which is what lets this sit in a cached incremental
    /// model at all - see <c>GrpcModelPlanShapeTests</c>. It is also the natural form: the encoding
    /// depends on the file's contents, so it has to be computed while the tree is in hand.
    /// </para>
    /// <para>
    /// Note the consequence for caching, which is inherent rather than a flaw: the data embeds a
    /// checksum of the whole file, so <em>any</em> edit to a file containing an intercepted call
    /// invalidates its sites. That is what the compiler demands - a stale checksum is CS9234 - so there
    /// is nothing to optimise here.
    /// </para>
    /// </remarks>
    internal sealed class GrpcInterceptSite : IEquatable<GrpcInterceptSite>
    {
        public GrpcInterceptSite(string contractFullName, string modelFullName,
            GrpcReceiverKind receiver, int locationVersion, string locationData)
        {
            ContractFullName = contractFullName;
            ModelFullName = modelFullName;
            Receiver = receiver;
            LocationVersion = locationVersion;
            LocationData = locationData;
        }

        /// <summary>The service contract, i.e. the call's type argument, fully qualified.</summary>
        public string ContractFullName { get; }

        /// <summary>The <c>[ProtoGrpc]</c> type whose <c>Instance</c> the call will be given.</summary>
        public string ModelFullName { get; }

        public GrpcReceiverKind Receiver { get; }

        public int LocationVersion { get; }

        /// <summary>The opaque <c>data</c> argument for <c>[InterceptsLocation]</c>.</summary>
        public string LocationData { get; }

        public bool Equals(GrpcInterceptSite? other)
            => other is not null
            && string.Equals(ContractFullName, other.ContractFullName, StringComparison.Ordinal)
            && string.Equals(ModelFullName, other.ModelFullName, StringComparison.Ordinal)
            && Receiver == other.Receiver
            && LocationVersion == other.LocationVersion
            && string.Equals(LocationData, other.LocationData, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as GrpcInterceptSite);

        public override int GetHashCode()
        {
            var hash = StringComparer.Ordinal.GetHashCode(LocationData);
            return (hash * -1521134295) + (int)Receiver;
        }
    }
}
