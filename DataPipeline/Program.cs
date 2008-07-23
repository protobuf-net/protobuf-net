using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MiscUtil.Linq;
using MiscUtil.Linq.Extensions;
using ProtoBuf;

namespace DataPipeline
{
    [ProtoContract]
    public class Foo
    {
        [ProtoMember(1)]
        public int Bar { get; set; }
    }

    [ProtoContract]
    public class FooWrapperInMemory
    {
        public FooWrapperInMemory() { Data = new List<Foo>(); }
        [ProtoMember(1)]
        public List<Foo> Data { get; private set; }
    }

    [ProtoContract]
    public class FooWrapperProducer
    {
        public FooWrapperProducer() { Data = new FooProducer(); }

        [ProtoMember(1)]
        public FooProducer Data { get; private set; }
    }

    // (only inheriting from Collection<Foo> as a lazy way of
    // implementing IList<Foo>
    public class FooProducer : Collection<Foo>, IDataProducer<Foo> {
        protected override void  InsertItem(int index, Foo item)
        {
            // note we don't pass it to the underlying collection!
            if (DataProduced != null) DataProduced(item);
        }
        public void End()
        {
            if (EndOfData != null) EndOfData();
        }

        public event Action<Foo> DataProduced;
        public event Action EndOfData;
    }

    class Program
    {
        static void Main()
        {
            FooWrapperProducer obj = new FooWrapperProducer();
            IDataProducer<Foo> producer = obj.Data;

            // count things *as they happen*
            // (this is a "future", not a value)
            var allCount = producer.Count();

            // some more interesting aggregates
            var justEven = from item in producer
                           where item.Bar % 2 == 0
                           select item;

            var evenCount = justEven.Count();
            var oddCount = producer.Count(x => x.Bar % 2 == 1);
            var evenAvgBar = justEven.Average(x=>x.Bar);

            // or a more bespoke operation
            justEven.DataProduced += x =>
            { 
                Console.WriteLine("Got a line: {0}", x.Bar);
            };

            // this could be loading an external file, etc
            using (Stream feed = CreateData())
            {
                Serializer.Merge(feed, obj);
            }

            // now we need to tell the listeners that
            // the feed is complete
            obj.Data.End();
            Console.WriteLine("All count: {0}", allCount.Value);
            Console.WriteLine("Even count: {0}", evenCount.Value);
            Console.WriteLine("Odd count: {0}", oddCount.Value);
            Console.WriteLine("Even average Bar: {0}", evenAvgBar.Value);
            // (with later versions don't need .Value)
        }
        static Stream CreateData()
        {
            // create data [just to simulate a data feed]
            FooWrapperInMemory inMem = new FooWrapperInMemory();
            for (int i = 0; i < 500; i++)
            {
                inMem.Data.Add(new Foo { Bar = i });
            }
            MemoryStream stream = new MemoryStream();
            // serialize and reset
            Serializer.Serialize(stream, inMem);
            stream.Position = 0;
            return stream;
        }
    }
}
