using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Benchmark
{
    [ClrJob, CoreJob]
    public class LibraryComparison
    {
        [Benchmark(Baseline = true)]
        public protoc.Database Google()
        {
            return protoc.Database.Parser.ParseFrom(_data);
        }

        private byte[] _data;

        [Benchmark(Description = "ROM")]
        public protogen.Database ROM()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                return (protogen.Database)model.Deserialize(ref state, reader, null, typeof(protogen.Database));
            }
        }
        [Benchmark(Description = "ROM*")]
        public protogen.Database ROM_Manual()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                protogen.Database obj = default;
                Merge(reader, ref state, ref obj);
                return obj;
            }
        }

        [Benchmark(Description = "MemoryStream")]
        public protogen.Database MemoryStream()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(out var state, new MemoryStream(_data), model))
            {
                return (protogen.Database)model.Deserialize(ref state, reader, null, typeof(protogen.Database));
            }
        }
        [Benchmark(Description = "MemoryStream*")]
        public protogen.Database MemoryStream_Manual()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(out var state, new MemoryStream(_data), model))
            {
                protogen.Database obj = default;
                Merge(reader, ref state, ref obj);
                return obj;
            }
        }

        private static void Merge(ProtoReader reader, ref ProtoReader.State state, ref protogen.Database obj)
        {
            SubItemToken tok;
            int field;
            if (obj == null) obj = new protogen.Database();
            while((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch(field)
                {
                    case 1:
                        do
                        {
                            protogen.Order _1 = default;
                            tok = ProtoReader.StartSubItem(ref state, reader);
                            Merge(reader, ref state, ref _1);
                            ProtoReader.EndSubItem(tok, reader);
                            obj.Orders.Add(_1);
                        } while (reader.TryReadFieldHeader(ref state, 1));
                        break;
                    default:
                        reader.AppendExtensionData(ref state, obj);
                        break;
                }
            }
        }
        private static void Merge(ProtoReader reader, ref ProtoReader.State state, ref protogen.Order obj)
        {
            SubItemToken tok;
            int field;
            if (obj == null) obj = new protogen.Order();
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1:
                        obj.OrderID = reader.ReadInt32(ref state);
                        break;
                    case 2:
                        obj.CustomerID = reader.ReadString(ref state);
                        break;
                    case 3:
                        obj.EmployeeID = reader.ReadInt32(ref state);
                        break;
                    case 4:
                        obj.OrderDate = BclHelpers.ReadTimestamp(ref state, reader);
                        break;
                    case 5:
                        obj.RequiredDate = BclHelpers.ReadTimestamp(ref state, reader);
                        break;
                    case 6:
                        obj.ShippedDate = BclHelpers.ReadTimestamp(ref state, reader);
                        break;
                    case 7:
                        obj.ShipVia = reader.ReadInt32(ref state);
                        break;
                    case 8:
                        obj.Freight = reader.ReadDouble(ref state);
                        break;
                    case 9:
                        obj.ShipName = reader.ReadString(ref state);
                        break;
                    case 10:
                        obj.ShipAddress = reader.ReadString(ref state);
                        break;
                    case 11:
                        obj.ShipCity = reader.ReadString(ref state);
                        break;
                    case 12:
                        obj.ShipRegion = reader.ReadString(ref state);
                        break;
                    case 13:
                        obj.ShipPostalCode = reader.ReadString(ref state);
                        break;
                    case 14:
                        obj.ShipCountry = reader.ReadString(ref state);
                        break;
                    case 15:
                        do
                        {
                            protogen.OrderLine _15 = default;
                            tok = ProtoReader.StartSubItem(ref state, reader);
                            Merge(reader, ref state, ref _15);
                            ProtoReader.EndSubItem(tok, reader);
                            obj.Lines.Add(_15);
                        } while (reader.TryReadFieldHeader(ref state, 1));
                        break;
                    default:
                        reader.AppendExtensionData(ref state, obj);
                        break;
                }
            }
        }

        private static void Merge(ProtoReader reader, ref ProtoReader.State state, ref protogen.OrderLine obj)
        {
            int field;
            if (obj == null) obj = new protogen.OrderLine();
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1:
                        obj.OrderID = reader.ReadInt32(ref state);
                        break;
                    case 2:
                        obj.ProductID = reader.ReadInt32(ref state);
                        break;
                    case 3:
                        obj.UnitPrice = reader.ReadDouble(ref state);
                        break;
                    case 4:
                        reader.Hint(WireType.SignedVariant);
                        obj.Quantity = reader.ReadInt32(ref state);
                        break;
                    case 5:
                        obj.Discount = reader.ReadSingle(ref state);
                        break;
                    default:
                        reader.AppendExtensionData(ref state, obj);
                        break;
                }
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            _data = File.ReadAllBytes("test.bin");
        }
    }
}
