
using System;
using System.Reflection;
namespace ProtoBuf
{
    internal static class SerializerCache<TValue>
    {
        static SerializerCache()
        {
            SimpleSerializers.Init();
        }
        private static ISerializer<TValue> @default, zigZag, twos, fixedSize;
        public static ISerializer<TValue> Default { get { return @default; } private set { @default = value; } }
        public static ISerializer<TValue> ZigZag { get { return zigZag; } private set { zigZag = value; } }
        public static ISerializer<TValue> TwosComplement { get { return twos; } private set { twos = value; } }
        public static ISerializer<TValue> FixedSize { get { return fixedSize; } private set { fixedSize = value; } }

        public static void Set(
            ISerializer<TValue> @default,
            ISerializer<TValue> zigZag,
            ISerializer<TValue> twosComplement,
            ISerializer<TValue> fixedSize)
        {
            Default = @default;
            ZigZag = zigZag;
            TwosComplement = twosComplement;
            FixedSize = fixedSize;
        }

        public static ISerializer<TValue> GetSerializer(DataFormat format)
        {
            ISerializer<TValue> result;
            switch (format)
            {
                case DataFormat.Default:
                    result = Default;
                    break;
                case DataFormat.FixedSize:
                    result = FixedSize;
                    break;
                case DataFormat.TwosComplement:
                    result = TwosComplement;
                    break;
                case DataFormat.ZigZag:
                    result = ZigZag;
                    break;
                default:
                    throw new NotSupportedException("Unknown data-format: " + format.ToString());
            }
            if (result == null && Default == null)
            {
                // not yet initialized
                if (Serializer.IsEntityType(typeof(TValue)))
                {
                    result = (ISerializer<TValue>)Activator.CreateInstance(
                        typeof(EntitySerializer<>).MakeGenericType(typeof(TValue)));
                }
                else if (typeof(TValue).IsEnum)
                {
                    Type underlying = Enum.GetUnderlyingType(typeof(TValue));
                    object baseSer = typeof(SerializerCache<>)
                        .MakeGenericType(underlying)
                        .GetMethod("GetSerializer", BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, new object[] { format });

                    Type[] ctorArgTypes = { typeof(ISerializer<>).MakeGenericType(underlying) };
                    result = (ISerializer<TValue>) typeof(EnumSerializer<,>)
                        .MakeGenericType(typeof(TValue), underlying)
                        .GetConstructor(ctorArgTypes).Invoke(new object[] { baseSer });
                }
                Default = result;
            }

            if (result == null) 
            {
                // tell the developer that they screwed up...
                Type nullType = Nullable.GetUnderlyingType(typeof(TValue));
                string name = nullType == null ? typeof(TValue).Name : ("Nullable-of-" + nullType.Name);

                string errorMsg = Default == null
                    ? "No serializers registered for {1}"
                    : "No {0} seriailzer registered for {1}";

                throw new InvalidOperationException(
                    string.Format(
                        errorMsg,
                        format,
                        name));
            }

            return result;
        }
    }
}
