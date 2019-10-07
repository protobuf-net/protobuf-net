namespace ProtoBuf.Internal
{
    internal sealed partial class PrimaryTypeProvider
    {
       

        //internal static object TryGetRepeatedProvider(Type type)
        //{
        //    if (type.IsValueType || type.IsArray) return null;

        //    if (TypeHelper.ResolveUniqueEnumerableT(type, out var t))
        //    {
        //        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        //        {
        //            var targs = t.GetGenericArguments();
        //            return Activator.CreateInstance(
        //                typeof(DictionarySerializer<,,>).MakeGenericType(type, targs[0], targs[1]), nonPublic: true);
        //        }

        //        return Activator.CreateInstance(
        //                typeof(EnumerableSerializer<,>).MakeGenericType(type, t), nonPublic: true);
        //    }
        //    return null;
        //}
    }
}
