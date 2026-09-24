#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    public sealed partial class GrpcProxyGenerator
    {
        private const string GrpcClientFactoryTypeName = "ProtoBuf.Grpc.Client.GrpcClientFactory";
        private const string CreateGrpcServiceName = "CreateGrpcService";
        private const string CallInvokerTypeName = "Grpc.Core.CallInvoker";
        private const string ChannelBaseTypeName = "Grpc.Core.ChannelBase";

        /// <summary>
        /// The cheap syntactic filter: <c>something.CreateGrpcService&lt;X&gt;(...)</c>.
        /// </summary>
        /// <remarks>
        /// This generator's other trigger is <c>ForAttributeWithMetadataName</c>, which costs nothing when
        /// unwanted; this one cannot be, since a call site carries no attribute. So the predicate has to
        /// be tight - it runs for every node in the compilation - and does no semantic work at all: one
        /// pattern match, rejecting on the method name before anything else.
        /// </remarks>
        internal static bool IsCreateGrpcServiceCandidate(SyntaxNode node)
            => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.ValueText: CreateGrpcServiceName,
                        TypeArgumentList.Arguments.Count: 1,
                    },
                },
            };

        /// <summary>
        /// Reduce a candidate call site to the data needed to intercept it, or null to leave it alone.
        /// </summary>
        /// <remarks>
        /// Deliberately says nothing here about whether interception is *wanted* - whether the feature is
        /// switched on, and whether any model covers the contract, are decided later, where the models and
        /// the parse options are in hand. This step only answers "is this a plain CreateGrpcService call
        /// naming a concrete contract, and where is it".
        /// </remarks>
        private static GrpcInterceptCandidate? ParseInterceptSite(GeneratorSyntaxContext ctx,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ctx.Node is not InvocationExpressionSyntax invocation) return null;
            if (ctx.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                return null;
            }

            // the extension is called in reduced form, so the declaring type is on ReducedFrom
            var declared = method.ReducedFrom ?? method.OriginalDefinition;
            if (declared.ContainingType?.ToDisplayString() != GrpcClientFactoryTypeName) return null;
            if (declared.Name != CreateGrpcServiceName) return null;
            if (method.TypeArguments.Length != 1) return null;

            // An open type argument has no location to name and could not be given one contract's proxy
            // anyway; the API would refuse it too, but rejecting here keeps the reason legible.
            if (method.TypeArguments[0] is not INamedTypeSymbol contract) return null;
            if (contract.TypeKind == TypeKind.Error) return null;
            if (ContainsTypeParameter(contract)) return null;

            // A call that already passes a factory is left alone: that consumer has done the thing we
            // would otherwise be doing for them. An omitted optional argument is the plain form, and an
            // explicit null says the same thing out loud.
            if (!IsPlainCall(invocation, ctx.SemanticModel, cancellationToken)) return null;

            var receiver = ReceiverKind(method);
            if (receiver is null) return null;

            if (InterceptableLocations.TryGet(ctx.SemanticModel, invocation, cancellationToken)
                is not (int version, string data))
            {
                // either the host predates the API, or it declined this site; both mean "leave it"
                return null;
            }

            return new GrpcInterceptCandidate(
                contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                receiver.Value, version, data, Where(invocation));
        }

        private static bool IsPlainCall(InvocationExpressionSyntax invocation, SemanticModel model,
            CancellationToken cancellationToken)
        {
            // reduced form, so the receiver is not in the argument list: no arguments means no factory
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count == 0) return true;

            foreach (var argument in arguments)
            {
                var constant = model.GetConstantValue(argument.Expression, cancellationToken);
                if (constant is { HasValue: true, Value: null }) continue;
                return false;
            }
            return true;
        }

        private static GrpcReceiverKind? ReceiverKind(IMethodSymbol method)
        {
            var receiver = method.ReducedFrom is { } reduced && reduced.Parameters.Length != 0
                ? reduced.Parameters[0].Type
                : method.ReceiverType;

            return receiver?.ToDisplayString() switch
            {
                CallInvokerTypeName => GrpcReceiverKind.CallInvoker,
                ChannelBaseTypeName => GrpcReceiverKind.ChannelBase,
                _ => null,
            };
        }

        private static Location? Where(SyntaxNode node) => node.GetLocation();
    }

    /// <summary>
    /// A call site plus the location it was found at, which the plan itself must not carry.
    /// </summary>
    /// <remarks>
    /// The location is here rather than on <see cref="GrpcInterceptSite"/> for the reason the whole model
    /// follows: a cached value must hold no Roslyn references. This wrapper never enters the plan - the
    /// diagnostics track takes the location and the emit track takes the site.
    /// </remarks>
    internal sealed class GrpcInterceptCandidate
    {
        public GrpcInterceptCandidate(string contractFullName, GrpcReceiverKind receiver,
            int locationVersion, string locationData, Location? location)
        {
            ContractFullName = contractFullName;
            Receiver = receiver;
            LocationVersion = locationVersion;
            LocationData = locationData;
            Location = location;
        }

        public string ContractFullName { get; }
        public GrpcReceiverKind Receiver { get; }
        public int LocationVersion { get; }
        public string LocationData { get; }
        public Location? Location { get; }
    }
}
