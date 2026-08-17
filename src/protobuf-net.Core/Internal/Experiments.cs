namespace ProtoBuf.Internal
{
    // example usage:
    // [Experimental(Experiments.SomeFeature, UrlFormat = Experiments.UrlFormat)]
    // where the id has a corresponding /docs/exp/{id}.md page telling people how to opt in
    internal static class Experiments
    {
        // note: {0} is substituted with the DiagnosticId by the compiler, e.g. .../exp/PBN9001
        //
        // the page name must match the id's *case* - GitHub Pages paths are case-sensitive, so
        // docs/exp/PBN9001.md, not docs/exp/pbn9001.md
        public const string UrlFormat = "https://docs.protobuf-net.dev/exp/{0}";

        /// <summary>
        /// Compile-time serializers: <c>[ProtoModel]</c>, <c>[ProtoSerializable]</c>,
        /// <c>[ProtoSurrogate]</c>.
        /// </summary>
        /// <remarks>
        /// protobuf-net.Grpc's <c>[ProtoGrpc]</c> deliberately shares this id: the two halves are
        /// opted into together in practice, so one <c>NoWarn</c> covering both is a feature rather
        /// than an accident. Do not reuse it for anything unrelated.
        /// </remarks>
        public const string CompileTimeModel = "PBN9001";
    }
}
