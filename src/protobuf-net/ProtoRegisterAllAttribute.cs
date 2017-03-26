using System;

namespace ProtoBuf
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class  ProtoRegisterAllAttribute : Attribute
    {
        private readonly string assemblyName;
        public ProtoRegisterAllAttribute(string assemblyName)
        {
            this.assemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        }
        public string AssemblyName => assemblyName;
    }
}
