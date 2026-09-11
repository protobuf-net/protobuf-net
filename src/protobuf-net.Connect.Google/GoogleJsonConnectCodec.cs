using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoBuf.Connect.Google
{
    /// <summary>
    /// The <c>json</c> codec for Google.Protobuf messages: the canonical JSON mapping.
    /// </summary>
    /// <remarks>
    /// Connect's JSON is not "some JSON" - the specification says to "use the canonical JSON mapping",
    /// which is protojson, the same thing every other implementation produces. So this delegates to
    /// Google's own <see cref="JsonFormatter"/> and <see cref="JsonParser"/> rather than reimplementing
    /// a specification that already has one correct implementation in this process.
    /// <para>
    /// Verified under native AOT: it publishes with no warnings of its own and round-trips correctly.
    /// That was worth measuring rather than assuming - <c>ReflectionUtil</c> builds field accessors and
    /// looked certain to sink it. See notes/connect/findings.md §46.
    /// </para>
    /// </remarks>
    public sealed class GoogleJsonConnectCodec : ConnectCodec
    {
        /// <summary>A shared instance using the default formatter and parser settings.</summary>
        public static GoogleJsonConnectCodec Instance { get; } = new();

        private readonly JsonFormatter? _formatter;
        private readonly JsonParser? _parser;

        /// <summary>
        /// Creates a codec. Supply a formatter and parser to override the defaults, including the type
        /// registry; omit them and one is derived per message type.
        /// </summary>
        public GoogleJsonConnectCodec(JsonFormatter? formatter = null, JsonParser? parser = null)
        {
            _formatter = formatter;
            _parser = parser;
        }

        /// <inheritdoc/>
        public override string Name => "json";

        /// <inheritdoc/>
        /// <remarks>
        /// Always <c>null</c>: the encoded length is not knowable without encoding. The framing copes -
        /// <see cref="ConnectEnvelope.WriteMessage"/> buffers when a codec cannot measure - and a unary
        /// response simply states no <c>Content-Length</c>.
        /// </remarks>
        protected override long? MeasureCore<T>(T value, IConnectMessageCodec<T>? over) => null;

        /// <inheritdoc/>
        protected override void WriteCore<T>(IBufferWriter<byte> destination, T value, IConnectMessageCodec<T>? over)
        {
            if (value is not IMessage message)
            {
                throw new NotSupportedException(
                    $"'{typeof(T).Name}' is not a Google.Protobuf message, so it has no canonical JSON form. "
                    + $"{nameof(GoogleJsonConnectCodec)} serves contract-first services; a code-first model needs its own JSON codec.");
            }

            // Format writes to a TextWriter, so there is a transcode here that a UTF-8 writer would not
            // need. Correctness first; this is the obvious thing to optimise if JSON ever matters for
            // throughput.
            var json = FormatterFor(message.Descriptor).Format(message);
            var bytes = Encoding.UTF8.GetByteCount(json);

            var span = destination.GetSpan(bytes);
            Encoding.UTF8.GetBytes(json, span);
            destination.Advance(bytes);
        }

        /// <inheritdoc/>
        protected override T ReadCore<T>(in ReadOnlySequence<byte> source, IConnectMessageCodec<T>? over)
        {
            var descriptor = DescriptorFor(over);
            var json = source.IsSingleSegment
                ? Encoding.UTF8.GetString(source.First.Span)
                : Encoding.UTF8.GetString(source.ToArray());

            return (T)ParserFor(descriptor).Parse(json, descriptor);
        }

        /// <summary>
        /// Finds the message descriptor for <typeparamref name="T"/>, without reflecting.
        /// </summary>
        /// <remarks>
        /// This is the whole trick, and it exists because the seam cannot be constrained: a per-method
        /// codec is <c>IConnectMessageCodec&lt;T&gt;</c> with no bound on <c>T</c>, and it cannot acquire
        /// one - <c>ServiceBinderBase.AddMethod</c> is constrained to <c>class</c>, so nothing downstream
        /// of a generated <c>BindService</c> can say more. Writing is fine (<c>Format</c> takes an
        /// instance), but parsing needs a descriptor and there is no instance to ask.
        /// <para>
        /// So one is manufactured: the binary <c>Marshaller&lt;T&gt;</c> the method already carries is
        /// asked to decode an <em>empty</em> message - a zero-length protobuf body is a valid message
        /// with every field at its default - and the resulting instance is asked for its descriptor. No
        /// reflection, no <c>Activator</c>, nothing for ILC to lose, and it is cached per type.
        /// </para>
        /// </remarks>
        private static MessageDescriptor DescriptorFor<T>(IConnectMessageCodec<T>? over)
        {
            if (Descriptors.TryGetValue(typeof(T), out var cached)) return cached;

            if (over is not MarshallerMessageCodec<T> marshaller)
            {
                throw new NotSupportedException(
                    $"No descriptor is available for '{typeof(T).Name}'. Canonical JSON needs one, and the only route "
                    + $"that does not reflect is the method's own {nameof(MarshallerMessageCodec<T>)} - so this codec "
                    + "serves methods built from a generated descriptor.");
            }

            // through the CODEC, not through Marshaller.Deserializer: a generated marshaller is built
            // from the contextual delegates, and the plain byte[] accessor throws NotImplementedException
            // for one. The codec already knows which of the pair to use.
            var empty = marshaller.Read(default);
            if (empty is not IMessage message)
            {
                throw new NotSupportedException(
                    $"'{typeof(T).Name}' is not a Google.Protobuf message, so it has no canonical JSON form.");
            }

            var descriptor = message.Descriptor;
            Descriptors[typeof(T)] = descriptor;
            return descriptor;
        }

        private static readonly ConcurrentDictionary<Type, MessageDescriptor> Descriptors = new();

        /// <summary>
        /// The formatter and parser for a message type, carrying a type registry that can resolve
        /// <c>google.protobuf.Any</c>.
        /// </summary>
        /// <remarks>
        /// <b>The default formatter and parser cannot handle <c>Any</c> at all</b>, and this is not an
        /// edge case: canonical JSON writes an <c>Any</c> as <c>{"@type": ..., ...}</c>, which means
        /// resolving that name to a descriptor in both directions. With an empty registry the parser
        /// says "Type registry has no descriptor for type name ...", which is how the conformance suite
        /// found it - its own payloads embed the echoed requests as <c>Any</c>.
        /// <para>
        /// The registry is built from the message's own <see cref="FileDescriptor"/>, which
        /// <c>TypeRegistry.FromFiles</c> walks together with its dependencies - so every type that file
        /// can name is resolvable, which for a service's own messages is the set that can appear. A
        /// consumer whose <c>Any</c> values come from elsewhere passes its own formatter and parser.
        /// </para>
        /// </remarks>
        private JsonFormatter FormatterFor(MessageDescriptor descriptor)
            => _formatter ?? Formatters.GetOrAdd(descriptor.File, static file
                => new JsonFormatter(new JsonFormatter.Settings(formatDefaultValues: false, RegistryFor(file))));

        private JsonParser ParserFor(MessageDescriptor descriptor)
            => _parser ?? Parsers.GetOrAdd(descriptor.File, static file
                => new JsonParser(new JsonParser.Settings(JsonParser.Settings.Default.RecursionLimit, RegistryFor(file))));

        private static TypeRegistry RegistryFor(FileDescriptor file) => TypeRegistry.FromFiles(file);

        private static readonly ConcurrentDictionary<FileDescriptor, JsonFormatter> Formatters = new();
        private static readonly ConcurrentDictionary<FileDescriptor, JsonParser> Parsers = new();
    }
}
