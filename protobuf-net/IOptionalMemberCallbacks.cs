namespace ProtoBuf
{
    /// <summary>
    /// Interface which allows more control over when optional members get serialized
    /// and provides notification when an optional member is deserialized.
    /// </summary>
    public interface IOptionalMemberCallbacks
    {
        /// <summary>
        /// Called prior to serialization of an optional member (of the type implementing this interface).
        /// The member will be serialized only if true is returned.
        /// </summary>        
        bool ShouldSerialize(int flag);

        /// <summary>
        /// Called after an optional member (of the type implementing this interface) is assigned a value
        /// from deserialization.
        /// </summary>
        void WasDeserialized(int flag);
    }
}
