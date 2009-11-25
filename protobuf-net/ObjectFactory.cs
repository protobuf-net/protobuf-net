using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ProtoBuf
{

    internal delegate T Ctor<T>();
    internal static class ObjectFactory {
        private static Ctor<T> MakeCtor<T>(Type concreteType) {
            ConstructorInfo ctor = concreteType.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Serializer.EmptyTypes, null);
            
            if(ctor == null) {
                string message = "No parameterless constructor found for " + typeof(T).Name;
                return delegate {
                    throw new ProtoException(message);
                };
            }
#if (CF || SILVERLIGHT)
            if (ctor.IsPublic) {
                if (ctor.DeclaringType == typeof(T)) {
                    return delegate { return Activator.CreateInstance<T>(); };
                } else { // for example, T=IList<string>, concrete=List<string>
                    Type finalType = ctor.DeclaringType;
                    return delegate { return (T) Activator.CreateInstance(finalType); };
                }
            }
            return delegate {
                return (T)ctor.Invoke(null);
            };
#else
            DynamicMethod dyn = new DynamicMethod("ctorWrapper", MethodAttributes.Static | MethodAttributes.Public,
                CallingConventions.Standard, typeof(T), Serializer.EmptyTypes, concreteType, true);
            ILGenerator il = dyn.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            return (Ctor<T>)dyn.CreateDelegate(typeof(Ctor<T>));
#endif
        }
        internal static Ctor<T> MakeCtor<T>() {
            // also handles IList<T> / IDictionary<T> etc
            Type type = GetConcreteType(typeof(T));
            return ObjectFactory.MakeCtor<T>(type);
        }

        private static Type GetConcreteType(Type type)
        {
            if (type.IsInterface)
            {
                if (type.IsGenericType)
                {
                    Type typeDef = type.GetGenericTypeDefinition(), listType = null;

                    if (typeDef == typeof(IList<>)) listType = typeof(List<>);
                    else if (typeDef == typeof(IDictionary<,>)) listType = typeof(Dictionary<,>);
                    if (listType != null)
                    {
                        type = listType.MakeGenericType(type.GetGenericArguments());
                    }
                }
            }
            return type;
        }

    }

    /// <summary>
    /// Abstract object factory, used to negate the need for a ": new()" generic constraint
    /// on Serializer-of-T.
    /// </summary>
    /// <typeparam name="T">The type of object to be created.</typeparam>
    internal abstract class ObjectFactory<T>
    {
        private static Ctor<T> impl;
        public static T Create() {
            if (impl == null) impl = ObjectFactory.MakeCtor<T>();
            return impl();
        }
    }
}