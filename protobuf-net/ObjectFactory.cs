using System;
using System.Reflection;

namespace ProtoBuf
{
    /// <summary>
    /// Abstract object factory, used to negate the need for a ": new()" generic constraint
    /// on Serializer-of-T.
    /// </summary>
    /// <typeparam name="T">The type of object to be created.</typeparam>
    internal abstract class ObjectFactory<T>
    {
        private static readonly ObjectFactory<T> impl;
        static ObjectFactory()
        {
            ConstructorInfo ctor = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, Serializer.EmptyTypes, null);

            if (ctor != null && ctor.IsPublic)
            {
                impl = (ObjectFactory<T>)typeof(ObjectFactory<T>)
                    .GetMethod("CreateGeneric").MakeGenericMethod(typeof(T)).Invoke(null, null);
            }
            else
            {
                impl = new ConstructorFactory<T>(ctor);
            }
        }
        /// <summary>
        /// Create a new instance of the given type.
        /// </summary>
        /// <param name="invokeOnDeserializing">Should the ISerializerCallback.OnDeserializing method be invoked (if available).</param>
        /// <returns></returns>
        public static T Create(bool invokeOnDeserializing)
        {
            T value = impl.CreateImpl();

            if (invokeOnDeserializing)
            {
                ISerializerCallback callback = value as ISerializerCallback;
                if (callback != null) callback.OnDeserializing();
            }

        return value;
        }
        protected abstract T CreateImpl();

        /// <summary>
        /// Wrapper method used via reflection to add the ": new()" constraint at
        /// runtime. In practice, TActual is always T once we have verified that
        /// there is a public parameterless constructor.
        /// </summary>
        /// <remarks>
        /// This method is public (not private) due to the reflection demands of
        /// (for example) Silverlight.
        /// </remarks>
        public static ObjectFactory<TActual> CreateGeneric<TActual>() where TActual : new()
        {
            return new GenericFactory<TActual>();            
        }
    }

    /// <summary>
    /// Represents a factory for creating objects with a public parameterless constructor;
    /// the "new()" generic constraint provides an optimisation over reflection.
    /// </summary>
    /// <typeparam name="T">The type of object to be created.</typeparam>
    internal sealed class GenericFactory<T> : ObjectFactory<T> where T : new()
    {
        protected override T CreateImpl()
        {
            return new T();
        }        
    }

    /// <summary>
    /// Represents a factory for creating objects with a non-public parameterless
    /// constructor (via reflection), or for lazily throwing a suitable error
    /// if no such constructor exists.
    /// </summary>
    /// <typeparam name="T">The type of object to be created.</typeparam>
    internal sealed class ConstructorFactory<T> : ObjectFactory<T>
    {
        private readonly ConstructorInfo ctor;
        public ConstructorFactory(ConstructorInfo ctor)
        {
            this.ctor = ctor;
        }
        protected override T CreateImpl()
        {
            if (ctor == null) throw new ProtoException("No parameterless constructor found for " + typeof(T).Name);
            return (T) ctor.Invoke(null);
        }
    }
}
