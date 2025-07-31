﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Examples.Issues;
using Xunit;
using Proto;
using ProtoBuf.Meta;
using Types;


namespace Examples.Issues
{
    
    public class SO9151111_b
    {
        [Fact]
        public void Execute()
        {
            var p = new SeniorDeveloper<bool> { Id = 1, Name = "x", Boaring = true, VeryBoaring = true };
            byte[] buf;
            SeniorDeveloper<bool> p2;

            _ = TimeSpan.Parse("01:10:00");

            var s = new ProtoBufModalSerializer(1);

            using (var f = new FileStream("protoTest.txt", FileMode.OpenOrCreate))
            {
                buf = s.Serialize(p);

                f.Write(buf, 0, buf.Length);
                f.Flush();
            }

            Console.WriteLine("Serialized... Buf length={0}", buf.Length.ToString());

            Console.WriteLine("Deserializing...");

            using (var f = new FileStream("protoTest.txt", FileMode.OpenOrCreate))
            {
                int offset = 0, read;
                while ((read = f.Read(buf, offset, buf.Length - offset)) > 0)
                {
                    offset += read;
                }
                Assert.Equal(offset, buf.Length);

                p2 = (SeniorDeveloper<bool>)s.Deserialize(buf, typeof(SeniorDeveloper<bool>));

                //p2 = (Person)s.Deserialize(buf, typeof(Person));
            }

            Console.WriteLine(p2.ToString());
        }
    }
}

namespace Proto
{
    public class ProtoBufModalSerializer
    {
        protected RuntimeTypeModel _modal;
        private readonly int label;
        public ProtoBufModalSerializer(int label)
        {
            this.label = label;
            this._modal = RuntimeTypeModel.Create();
            //this._modal.AutoAddMissingTypes = true;
            this.Init();
        }

        private void Init()
        {
            Dictionary<Type, List<Type>> repo = new Dictionary<Type, List<Type>>();

            foreach (var lValue in this.GetDCTypes("Types"))
            {
#if COREFX
                var type = lValue.GetTypeInfo();
#else
                var type = lValue;
#endif

                if (type.IsGenericTypeDefinition == false)
                {
                    Console.WriteLine("{0}: Processing: {1} ({2})", label, type.Name, type.IsGenericTypeDefinition);
                    var meta = this._modal.Add(lValue, false)
                                 .Add(this.GetDMProperties(lValue).Select(p => p.Name)
                                 .ToArray());

                    this.SetCallbacks(meta);

                    if (type.BaseType != null && type.BaseType != typeof(Object))
                    {
                        if (!repo.TryGetValue(type.BaseType, out var childs))
                        {
                            childs = new List<Type>();
                            repo.Add(type.BaseType, childs);
                        }

                        childs.Add(lValue);
                    }


                    foreach (var parent in repo.Keys)
                    {
                        if (this._modal.IsDefined(parent))
                        {
                            var metaType = _modal[parent];
                            Assert.NotNull(metaType); //, "meta");

                            int i = 500;

                            foreach (var child in repo[parent].OrderBy(t => t.Name))
                            {
                                Console.WriteLine("{0}: Adding {1} => {2}", label, parent.Name, child.Name);
                                if (!metaType.GetSubtypes().Any(x => x.DerivedType.Type == child))
                                {
                                    metaType.AddSubType(i++, child);
                                }
                            }
                        }
                    }
                }
            }

            this._modal.CompileInPlace();
        }



        public virtual object Deserialize(byte[] serializedObj, Type objectType)
        {
            using var memStream = new MemoryStream(serializedObj);
            //return Serializer.NonGeneric.Deserialize(objectType, memStream);
#pragma warning disable CS0618
            return this._modal.Deserialize(memStream, null, objectType);
#pragma warning restore CS0618
        }

        public virtual byte[] Serialize(object obj)
        {
            return this.Serialize(obj.GetType(), obj);
        }

        public virtual byte[] Serialize(Type objectType, object obj)
        {
            using var memStream = new MemoryStream();
            //Serializer.NonGeneric.Serialize(memStream, obj);
#pragma warning disable CS0618
            this._modal.Serialize(memStream, obj);
#pragma warning restore CS0618

            return memStream.ToArray();
        }

        public virtual TType Deserialize<TType>(byte[] serializedObj)
        {
            using var memStream = new MemoryStream(serializedObj);
            //return Serializer.Deserialize<TType>(memStream);
#pragma warning disable CS0618
            return (TType)this._modal.Deserialize(memStream, null, typeof(TType));
#pragma warning restore CS0618
        }


        private IEnumerable<Type> GetDCTypes(string assemblyName)
        {
#if COREFX
            var assembly = typeof(SO9151111_b).GetTypeInfo().Assembly;
#else
            var assembly = typeof(SO9398578).Assembly;
#endif

            foreach (var type in assembly.GetTypes().Where(t => t.Namespace == "Types"))
            {
#if COREFX
                var tmp = type.GetTypeInfo();
#else
                var tmp = type;
#endif
                if (tmp.IsDefined(typeof(DataContractAttribute), false))
                    yield return type;
            }
        }

        private IEnumerable<PropertyInfo> GetDMProperties(Type dcType)
        {
            foreach (var prop in dcType.GetProperties())
            {
                if (prop.IsDefined(typeof(DataMemberAttribute), false))
                    yield return prop;
            }
        }

        private void SetCallbacks(MetaType meta)
        {
            MethodInfo beforeDeserialized = null;
            MethodInfo afterDeserialized = null;
            MethodInfo beforeSerialized = null;
            MethodInfo afterSerialized = null;

            foreach (var method in meta.Type.GetMethods())
            {
                beforeDeserialized = method.IsDefined(typeof(OnDeserializingAttribute), false) ? method : beforeDeserialized;
                afterDeserialized = method.IsDefined(typeof(OnDeserializedAttribute), false) ? method : afterDeserialized;
                beforeSerialized = method.IsDefined(typeof(OnSerializingAttribute), false) ? method : beforeSerialized;
                afterSerialized = method.IsDefined(typeof(OnSerializedAttribute), false) ? method : afterSerialized;
            }

            meta.SetCallbacks(beforeSerialized, afterSerialized, beforeDeserialized, afterDeserialized);
        }
    }
}

namespace Types
{
    //[ProtoContract]
    [DataContract]
    //[ProtoInclude(500, typeof(Developer))]
    public class Person
    {
        //[ProtoMember(1)]
        [DataMember]
        public int Id { get; set; }
        //[ProtoMember(2)]
        [DataMember]
        public string Name { get; set; }

        public override string ToString()
        {
            return "Id=" + Id.ToString() + " Name=" + Name;
        }

        [OnDeserialized]
        public void OnDesr(StreamingContext c) => OnDesr();

        [OnSerializing]
        public void OnSer(StreamingContext c) => OnSer();

        protected virtual void OnDesr()
        {
            Console.WriteLine("OnDeserialized");
        }
        protected virtual void OnSer() { }
    }

    [DataContract]
    //[ProtoContract]
    public class Developer : Person
    {
        [DataMember]
        //[ProtoMember(1)]
        public bool Boaring { get; set; }

        public bool XBoaring { get; set; }

        public override string ToString()
        {
            return base.ToString() + " Boaring=" + Boaring.ToString() + " XBoaring=" + XBoaring.ToString();
        }
    }

    [DataContract]
    public class SeniorDeveloper<T> : Developer
    {
        [DataMember]
        //[ProtoMember(1)]
        public T VeryBoaring { get; set; }

        public override string ToString()
        {
            return base.ToString() + " VeryBoaring=" + VeryBoaring.ToString();
        }

        protected override void OnDesr()
        {
            Console.WriteLine("OnDeserialized");
            base.OnDesr();
        }

        protected override void OnSer()
        {
            Console.WriteLine("OnSerializing");
            base.OnSer();
        }
    }
}
