#if !NO_RUNTIME
using System;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Represents an inherited type in a type hierarchy.
    /// </summary>
    public sealed class SubType
    {
        private readonly int fieldNumber;
        /// <summary>
        /// The field-number that is used to encapsulate the data (as a nested
        /// message) for the derived dype.
        /// </summary>
        public int FieldNumber { get { return fieldNumber; } }
        /// <summary>
        /// The sub-type to be considered.
        /// </summary>
        public MetaType DerivedType { get { return derivedType; } }
        private readonly MetaType derivedType;

        /// <summary>
        /// Creates a new SubType instance.
        /// </summary>
        /// <param name="fieldNumber">The field-number that is used to encapsulate the data (as a nested
        /// message) for the derived dype.</param>
        /// <param name="derivedType">The sub-type to be considered.</param>
        /// <param name="format">Specific encoding style to use; in particular, Grouped can be used to avoid buffering, but is not the default.</param>
        public SubType(int fieldNumber, MetaType derivedType, DataFormat format)
        {
            if (derivedType == null) throw new ArgumentNullException("derivedType");
            if (fieldNumber <= 0) throw new ArgumentOutOfRangeException("fieldNumber");
            this.fieldNumber = fieldNumber;
            this.derivedType = derivedType;
            this.dataFormat = format;
        }

        private readonly DataFormat dataFormat;

        private IProtoSerializer serializer;
        internal IProtoSerializer Serializer
        {
            get
            {
                if (serializer == null) serializer = BuildSerializer();
                return serializer;
            }
        }

        private IProtoSerializer BuildSerializer()
        {
            // note the caller here is MetaType.BuildSerializer, which already has the sync-lock
            WireType wireType = WireType.String;
            if(dataFormat == DataFormat.Group) wireType = WireType.StartGroup; // only one exception
            
            IProtoSerializer ser = new SubItemSerializer(derivedType.Type, derivedType.GetKey(false, false), derivedType, false);
            return new TagDecorator(fieldNumber, wireType, false, ser);
        }
    }
}
#endif