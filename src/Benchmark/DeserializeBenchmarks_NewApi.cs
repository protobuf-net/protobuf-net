#if NEW_API
using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Benchmark
{
    partial class DeserializeBenchmarks
    {

        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "Default")]
        public protogen.Database ROM_Default() => ROM_New(RuntimeTypeModel.Default);

        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "CIP")]
        public protogen.Database ROM_CIP() => ROM_New(_cip);

        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "C")]
        public protogen.Database ROM_C() => ROM_New(_c);

        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "Auto")]
        public protogen.Database ROM_Auto() => ROM_New(_auto);

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "Default")]
        public protogen.Database MemoryStream_New_Default() => MemoryStream_New(RuntimeTypeModel.Default);

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "CIP")]
        public protogen.Database MemoryStream_New_CIP() => MemoryStream_New(_cip);

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "C")]
        public protogen.Database MemoryStream_New_C() => MemoryStream_New(_c);

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "Auto")]
        public protogen.Database MemoryStream_New_Auto() => MemoryStream_New(_auto);

        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "Dll")]
        public protogen.Database ROM_Dll() => ROM_New(_dll);

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "Dll")]
        public protogen.Database MS_Dll() => MemoryStream_New(_dll);

        private protogen.Database MemoryStream_New(TypeModel model)
        {
            using var reader = ProtoReader.Create(out var state, ExposableData(), model ?? Throw());
            return reader.Deserialize<protogen.Database>(ref state);
        }

        private protogen.Database ROM_New(TypeModel model)
        {
            using var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model ?? Throw());
            return reader.Deserialize<protogen.Database>(ref state);
        }


        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "Manual")]
        public protogen.Database ROM_Manual()
        {
            using var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), RuntimeTypeModel.Default);
            protogen.Database obj = default;
            Merge(reader, ref state, ref obj);
            return obj;
        }

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "Manual")]
        public protogen.Database MemoryStream_Manual()
        {
            using var reader = ProtoReader.Create(out var state, ExposableData(), RuntimeTypeModel.Default);
            protogen.Database obj = default;
            Merge(reader, ref state, ref obj);
            return obj;
        }

        public TypeModel Auto => _auto;

        private static void Merge(ProtoReader reader, ref ProtoReader.State state, ref protogen.Database obj)
        {
            SubItemToken tok;
            int field;
            if (obj == null) obj = new protogen.Database();
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1:
                        do
                        {
                            protogen.Order _1 = default;
                            tok = ProtoReader.StartSubItem(reader, ref state);
                            Merge(reader, ref state, ref _1);
                            ProtoReader.EndSubItem(tok, reader, ref state);
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
                        obj.OrderDate = BclHelpers.ReadTimestamp(reader, ref state);
                        break;
                    case 5:
                        obj.RequiredDate = BclHelpers.ReadTimestamp(reader, ref state);
                        break;
                    case 6:
                        obj.ShippedDate = BclHelpers.ReadTimestamp(reader, ref state);
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
                            tok = ProtoReader.StartSubItem(reader, ref state);
                            Merge(reader, ref state, ref _15);
                            ProtoReader.EndSubItem(tok, reader, ref state);
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

    }
}
#endif
