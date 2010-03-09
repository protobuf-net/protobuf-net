#warning excised
//#if NET_3_0
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Runtime.Serialization;
//using System.ServiceModel;
//using NUnit.Framework;
//using ProtoBuf.ServiceModel;

//namespace Examples.ServiceModel
//{


//    [DataContract]
//    public class MyData {
//        public MyData() { SubData = new List<MySubData>(); }
//        [DataMember(Order = 1)]
//        public List<MySubData> SubData {get; private set;}
//    }
//    [DataContract]
//    public class MySubData
//    {
//        [DataMember(Order = 1)]
//        public string Name { get; set; }
//        [DataMember(Order = 2)]
//        public int Number { get; set; }
//    }

//    [ServiceContract]
//    interface IMyService
//    {
//        [OperationContract, ProtoBehavior]
//        MyData UsingProtoItem(MyData data);

//        [OperationContract]
//        MyData RegularWcfItem(MyData data);

//        [OperationContract, ProtoBehavior]
//        MyData[] UsingProtoList(List<MyData> data);

//        [OperationContract]
//        MyData[] RegularWcfList(List<MyData> data);

//        [OperationContract]
//        bool Ping();

//        [OperationContract]
//        string SimpleTypesRegular(int value);
//        [OperationContract, ProtoBehavior]
//        string SimpleTypesProto(int value);
//    }
//    class MyService : IMyService
//    {
//        public MyData RegularWcfItem(MyData data) { return data; }
//        public MyData UsingProtoItem(MyData data) { return data; }
//        public MyData[] RegularWcfList(List<MyData> data) { return data == null ? null : data.ToArray(); }
//        public MyData[] UsingProtoList(List<MyData> data) { return data == null ? null : data.ToArray(); }

//        public bool Ping() { return true; }

//        public string SimpleTypesRegular(int value)
//        {
//            return value.ToString();
//        }
//        public string SimpleTypesProto(int value)
//        {
//            return value.ToString();
//        }
//    }


//    [TestFixture]
//    public class WcfTest
//    {
//        ServiceHost host;
//        [TestFixtureSetUp]
//        public void StartServer()
//        {
//            try
//            {
//                StopServer();
//                host = new ServiceHost(typeof(MyService),
//                    new Uri("http://localhost:84/MyService"));
//                host.Open();
//            }
//            catch(Exception ex)
//            {
//                Console.WriteLine(ex.GetType().FullName);
//                Console.WriteLine(ex.Message);
//                throw;
//            }
//        }

//        [TestFixtureTearDown]
//        public void StopServer()
//        {
//            if (host != null)
//            {
//                host.Close();
//                host = null;
//            }
//        }

//        static WcfProxy<IMyService> GetProxy()
//        {
//            return new WcfProxy<IMyService>();
//        }
//        [Test]
//        public void Ping()
//        {
//            using (var proxy = GetProxy())
//            {
//                Assert.IsTrue(proxy.Service.Ping());
//            }
//        }
//        [Test]
//        public void SimpleTypesRegular()
//        {
//            using (var proxy = GetProxy())
//            {
//                Assert.AreEqual("27", proxy.Service.SimpleTypesRegular(27));
//            }
//        }

//        [Test]
//        public void SimpleTypesProto()
//        {
//            using (var proxy = GetProxy())
//            {
//                Assert.AreEqual("27", proxy.Service.SimpleTypesProto(27));
//            }
//        }

//        [Test]
//        public void WcfRegularNullToNull()
//        {
//            using (var proxy = GetProxy())
//            {
//                Assert.IsNull(proxy.Service.RegularWcfItem(null));
//            }
//        }

//        [Test]
//        public void WcfProtoNullToNull()
//        {
//            using (var proxy = GetProxy())
//            {
//                Assert.IsNull(proxy.Service.UsingProtoItem(null));
//            }
//        }

//        static List<MyData> GetShortList() {
//            List<MyData> list = new List<MyData>();
//            for(int j = 0 ; j < 5 ; j++) {
//                MyData data = new MyData();
//                for (int i = 0; i < 50; i++)
//                {
//                    data.SubData.Add(new MySubData { Number = i, Name = "item " + i.ToString() });
//                }
//                list.Add(data);
//            }
//            return list;
//        }
//        static void CheckListResult(MyData[] arr)
//        {
//            Assert.IsNotNull(arr, "Null");
//            Assert.AreEqual(5, arr.Length, "Length");
//            for (int j = 0; j < arr.Length; j++)
//            {
//                MyData item = arr[j];
//                Assert.IsNotNull(item, "Null: " + j.ToString());
//                Assert.IsNotNull(item.SubData, "SubData Null: " + j.ToString());
//                Assert.AreEqual(50, item.SubData.Count, "SubData Count: " + j.ToString());
//                for (int i = 0; i < item.SubData.Count; i++)
//                {
//                    var subItem = item.SubData[i];
//                    Assert.IsNotNull(subItem, i.ToString());
//                    Assert.AreEqual(i, subItem.Number, "Number: " + i.ToString());
//                    Assert.AreEqual("item " + i.ToString(), subItem.Name, "Name: " + i.ToString());
//                }
//            }
//        }
//        [Test]
//        public void RegularWcfList()
//        {
//            using (var proxy = GetProxy())
//            {
//                MyData[] arr = proxy.Service.RegularWcfList(GetShortList());
//                CheckListResult(arr);
//            }            
//        }
//        [Test]
//        public void ProtoList()
//        {
//            using (var proxy = GetProxy())
//            {
//                MyData[] arr = proxy.Service.UsingProtoList(GetShortList());
//                CheckListResult(arr);
//            }
//        }

//        [Test]
//        public void RunWcfTest()
//        {
//            // generate some data:
//            MyData data = new MyData();
//            for (int i = 0; i < 5000; i++)
//            {
//                data.SubData.Add(new MySubData { Number = i, Name = "item " + i.ToString() });
//            }

//            using (var proxy = GetProxy())
//            {
//                // for JIT etc
//                proxy.Service.RegularWcfItem(data);
//                proxy.Service.UsingProtoItem(data);

//                Stopwatch watchProto = Stopwatch.StartNew();
//                MyData dataProto = proxy.Service.UsingProtoItem(data);
//                watchProto.Stop();
//                Console.WriteLine("WCF: Proto took: {0}", watchProto.ElapsedMilliseconds);

//                Stopwatch watchRegular = Stopwatch.StartNew();
//                MyData dataRegular = proxy.Service.RegularWcfItem(data);
//                watchRegular.Stop();
//                Console.WriteLine("WCF: Regular took: {0}", watchRegular.ElapsedMilliseconds);

//                Assert.AreEqual(data.SubData.Count, dataProto.SubData.Count, "Proto count");
//                Assert.AreEqual(data.SubData.Count, dataRegular.SubData.Count, "Regular count");
//                for(int i = 0; i < data.SubData.Count ;i++) {
//                    Assert.AreEqual(data.SubData[i].Name, dataProto.SubData[i].Name, "Proto name");
//                    Assert.AreEqual(data.SubData[i].Number, dataProto.SubData[i].Number, "Proto number");
//                    Assert.AreEqual(data.SubData[i].Name, dataRegular.SubData[i].Name, "Regular name");
//                    Assert.AreEqual(data.SubData[i].Number, dataRegular.SubData[i].Number, "Regular number");                    
//                }
//                Console.WriteLine(string.Format("Validated: {0}", data.SubData.Count));

//                Assert.Less(watchProto.ElapsedMilliseconds, watchRegular.ElapsedMilliseconds, "Proto should be quicker");
//            }
//        }
//    }


//    public sealed class WcfProxy<T> : ClientBase<T>, IDisposable
//    where T : class
//    {
//        public T Service { get { return base.Channel; } }
//        public void Dispose()
//        {
//            try
//            {
//                switch (State)
//                {
//                    case CommunicationState.Closed:
//                        break; // nothing to do
//                    case CommunicationState.Faulted:
//                        Abort();
//                        break;
//                    case CommunicationState.Opened:
//                        Close();
//                        break;
//                }
//            }
//            catch { } // best efforts...
//        }
//    }
//}
//#endif