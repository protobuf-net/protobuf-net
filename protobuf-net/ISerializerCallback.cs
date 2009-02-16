
namespace ProtoBuf
{
    /// <summary>Allows an object to execute additional code during the serialization/deserialization proces.</summary>
    interface ISerializerCallback
    {
        /// <summary>Invoked before an object is serialized.</summary>
        void OnSerializing();
        /// <summary>Invoked after an object is serialized.</summary>
        void OnSerialized();
        /// <summary>Invoked before an object is deserialized.</summary>
        void OnDeserializing();
        /// <summary>Invoked after an object is deserialized.</summary>
        void OnDeserialized();
    }
}
