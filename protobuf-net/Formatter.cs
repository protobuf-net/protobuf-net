#if REMOTING

using System.Runtime.Serialization;
using System.IO;

namespace ProtoBuf
{
    internal sealed class Formatter<T> : IFormatter
        where T : class, new()
    {
        private SerializationBinder binder;
        public SerializationBinder Binder
        {
            get {return binder;}
            set {binder = value;}
        }

        private StreamingContext context;
        public StreamingContext Context
        {
            get { return context; }
            set { context = value; }
        }

        public object Deserialize(Stream serializationStream)
        {
            return Serializer.Deserialize<T>(serializationStream);
        }

        public void Serialize(Stream serializationStream, object graph)
        {
            Serializer.Serialize<T>(serializationStream, (T)graph);
        }

        private ISurrogateSelector surrogateSelector;
        public ISurrogateSelector SurrogateSelector
        {
            get { return surrogateSelector; }
            set { surrogateSelector = value; }
        }
    }
}
#endif