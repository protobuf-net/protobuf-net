using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue316
    {
        [Test]
        public void Execute()
        {
            var runtimeTypeModel = TypeModel.Create();
            var myExceptionType = typeof(MyException);
            var metaType = runtimeTypeModel.Add(myExceptionType, false);
            metaType.SetSurrogate(typeof(BinarySerializationSurrogate<>).MakeGenericType(myExceptionType));

            string proto = runtimeTypeModel.GetSchema(null);

            Assert.AreEqual(@"package Examples.Issues;

message BinarySerializationSurrogate_MyException {
   optional bytes objectData = 1;
}
", proto);
        }

        class MyException : Exception { }
        


 /// <summary>
    /// Surrogate class to allow Protobuf-net to serialize any class that implements ISerializeable
    /// (e.g. Exceptions).
    /// </summary>
    /// <typeparam name="T">The type of an object that implements ISerializeable.</typeparam>
    [ProtoContract]
    internal class BinarySerializationSurrogate<T>
    {
        [ProtoMember(1)]
        private byte[] objectData = null;

        public static implicit operator T(BinarySerializationSurrogate<T> surrogate)
        {
            T returnValue = default(T);
            if (surrogate == null)
            {
                return returnValue;
            }

            var serializer = new BinaryFormatter();
            using (var serializedStream = new MemoryStream(surrogate.objectData))
            {
                returnValue = (T)serializer.Deserialize(serializedStream);
            }

            return returnValue;
        }

        public static implicit operator BinarySerializationSurrogate<T>(T objectToSerialize)
        {
            if (objectToSerialize == null)
            {
                return null;
            }

            var returnValue = new BinarySerializationSurrogate<T>();

            var serializer = new BinaryFormatter();
            using (var serializedStream = new MemoryStream())
            {
                serializer.Serialize(serializedStream, objectToSerialize);
                returnValue.objectData = serializedStream.ToArray();
            }

            return returnValue;
        }
    }
    }
}
