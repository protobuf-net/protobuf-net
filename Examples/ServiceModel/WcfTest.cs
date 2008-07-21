using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using NUnit.Framework;
using System.Diagnostics;
using ProtoBuf.ServiceModel;

namespace Examples.ServiceModel
{


    [DataContract]
    public class MyData {
        public MyData() { SubData = new List<MySubData>(); }
        [DataMember(Order = 1)]
        public List<MySubData> SubData {get; private set;}
    }
    [DataContract]
    public class MySubData
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
        [DataMember(Order = 2)]
        public int Number { get; set; }
    }

    [ServiceContract]
    interface IMyService
    {
        [OperationContract, ProtoBehavior]
        MyData UsingProto(MyData data);

        [OperationContract]
        MyData RegularWcf(MyData data);

        [OperationContract]
        bool Ping();
    }
    class MyService : IMyService
    {
        public MyData RegularWcf(MyData data) { return data; }
        public MyData UsingProto(MyData data) { return data; }
        public bool Ping() { return true; }
    }


    [TestFixture]
    public class WcfTest
    {
        [Test]
        public void RunWcfTest()
        {
            // generate some data:
            MyData data = new MyData();
            for (int i = 0; i < 5000; i++)
            {
                data.SubData.Add(new MySubData { Number = i, Name = "item " + i.ToString() });
            }
            using (ServiceHost host = new ServiceHost(typeof(MyService),
                new Uri("http://localhost/MyService")))
            {
                host.Open();

                using (WcfProxy<IMyService> proxy = new WcfProxy<IMyService>())
                {
                    Assert.IsTrue(proxy.Service.Ping(), "ping");

                    MyData dataProto = proxy.Service.UsingProto(null);
                    Assert.IsNull(dataProto, "Proto null=>null");
                    Stopwatch watchProto = Stopwatch.StartNew();
                    dataProto = proxy.Service.UsingProto(data);
                    watchProto.Stop();
                    Console.WriteLine("WCF: Proto took: {0}", watchProto.ElapsedMilliseconds);

                    MyData dataRegular = proxy.Service.RegularWcf(null);
                    Assert.IsNull(dataRegular, "Regular null=>null");
                    Stopwatch watchRegular = Stopwatch.StartNew();
                    dataRegular = proxy.Service.RegularWcf(data);
                    watchRegular.Stop();
                    Console.WriteLine("WCF: Regular took: {0}", watchRegular.ElapsedMilliseconds);

                    Assert.AreEqual(data.SubData.Count, dataProto.SubData.Count, "Proto count");
                    Assert.AreEqual(data.SubData.Count, dataRegular.SubData.Count, "Regular count");
                    for(int i = 0; i < data.SubData.Count ;i++) {
                        Assert.AreEqual(data.SubData[i].Name, dataProto.SubData[i].Name, "Proto name");
                        Assert.AreEqual(data.SubData[i].Number, dataProto.SubData[i].Number, "Proto number");
                        Assert.AreEqual(data.SubData[i].Name, dataRegular.SubData[i].Name, "Regular name");
                        Assert.AreEqual(data.SubData[i].Number, dataRegular.SubData[i].Number, "Regular number");                    
                    }
                    Console.WriteLine(string.Format("Validated: {0}", data.SubData.Count));

                    Assert.Less(watchProto.ElapsedMilliseconds, watchRegular.ElapsedMilliseconds, "Proto should be quicker");
                }

                host.Close();
            }
        }
    }


    public sealed class WcfProxy<T> : ClientBase<T>, IDisposable
    where T : class
    {
        public T Service { get { return base.Channel; } }
        public void Dispose()
        {
            try
            {
                switch (State)
                {
                    case CommunicationState.Closed:
                        break; // nothing to do
                    case CommunicationState.Faulted:
                        Abort();
                        break;
                    case CommunicationState.Opened:
                        Close();
                        break;
                }
            }
            catch { } // best efforts...
        }
    }
}
