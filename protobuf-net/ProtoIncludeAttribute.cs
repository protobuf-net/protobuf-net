using System;
using System.ComponentModel;
using System.Reflection;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates the known-types to support for an individual
    /// message. This serializes each level in the hierarchy as
    /// a nested message to retain wire-compatibility with
    /// other protocol-buffer implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoIncludeAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="knownType">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, Type knownType)
            : this(tag, knownType == null ? "" : knownType.AssemblyQualifiedName) { }

        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="knownTypeName">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, string knownTypeName)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers");
            if (Helpers.IsNullOrEmpty(knownTypeName)) throw new ArgumentNullException("knownTypeName", "Known type cannot be blank");
            this.tag = tag;
            this.name = knownTypeName;
        }

        /// <summary>
        /// Gets the unique index (within the type) that will identify this data.
        /// </summary>
        public int Tag { get { return tag; } }
        private readonly int tag;

        /// <summary>
        /// Gets the additional type to serialize/deserialize.
        /// </summary>
        public string KnownTypeName { get { return name; } }
        private readonly string name;

        /// <summary>
        /// Gets the additional type to serialize/deserialize.
        /// </summary>
        public Type KnownType
        {
            get
            {
                return Type.GetType(KnownTypeName);
            }
        }

        internal Type ResolveKnownType(Assembly assembly)
        {
            if (Helpers.IsNullOrEmpty(KnownTypeName)) return null;
            try
            {
                Type type = Type.GetType(KnownTypeName);
                if (type != null) return type;
            }
            catch { }
            try
            {
                int i = KnownTypeName.IndexOf(',');
                string fullName = (i > 0 ? KnownTypeName.Substring(0, i) : KnownTypeName).Trim();
                Type type = (assembly == null ? Assembly.GetCallingAssembly() : assembly).GetType(fullName);
                if (type != null) return type;
            }
            catch { }
            return null;
        }


        /// <summary>
        /// Specifies whether the inherited sype's sub-message should be
        /// written with a length-prefix (default), or with group markers.
        /// </summary>
        [DefaultValue(DataFormat.Default)]
        public DataFormat DataFormat
        {
            get { return dataFormat; }
            set { dataFormat = value; }
        }
        private DataFormat dataFormat = DataFormat.Default;
    }
}
