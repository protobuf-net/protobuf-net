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

#pragma warning disable IDE0051 // Remove unused private members
        static void Dispose(IDisposable input) => input?.Dispose();
#pragma warning restore IDE0051 // Remove unused private members

        //[BenchmarkCategory("ROM_RefState")]
        //[Benchmark(Description = "C_Pooled")]
        //public void ROM_C_Pooled() => Dispose(ROM_NewPooled(_c));

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

        //[BenchmarkCategory("MS_RefState")]
        //[Benchmark(Description = "C_Pooled")]
        //public void MemoryStream_New_C_Pooled() => Dispose(MemoryStream_NewPooled(_c));

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
            using var state = ProtoReader.State.Create(ExposableData(), model ?? Throw());
            return state.DeserializeRoot<protogen.Database>();
        }

        private protogen.Database ROM_New(TypeModel model)
        {
            using var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), model ?? Throw());
            return state.DeserializeRoot<protogen.Database>();
        }

        //private protogen.pooled.Database MemoryStream_NewPooled(TypeModel model)
        //{
        //    using var reader = ProtoReader.State.Create(ExposableData(), model ?? Throw());
        //    return reader.Deserialize<protogen.pooled.Database>();
        //}

        //private protogen.pooled.Database ROM_NewPooled(TypeModel model)
        //{
        //    using var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), model ?? Throw());
        //    return state.Deserialize<protogen.pooled.Database>();
        //}


        [BenchmarkCategory("ROM_RefState")]
        [Benchmark(Description = "Manual")]
        public protogen.Database ROM_Manual()
        {
            var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), RuntimeTypeModel.Default);
            try
            {
                protogen.Database obj = default;
                Merge(ref state, ref obj);
                return obj;
            }
            finally
            {
                state.Dispose();
            }
        }

        [BenchmarkCategory("MS_RefState")]
        [Benchmark(Description = "Manual")]
        public protogen.Database MemoryStream_Manual()
        {
            var state = ProtoReader.State.Create(ExposableData(), RuntimeTypeModel.Default);
            try
            {
                protogen.Database obj = default;
                Merge(ref state, ref obj);
                return obj;
            }
            finally
            {
                state.Dispose();
            }
        }

        public TypeModel Auto => _auto;

        private static void Merge(ref ProtoReader.State state, ref protogen.Database obj)
        {
            SubItemToken tok;
            int field;
            if (obj == null) obj = new protogen.Database();
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 1:
                        do
                        {
                            protogen.Order _1 = default;
                            tok = state.StartSubItem();
                            Merge(ref state, ref _1);
                            state.EndSubItem(tok);
                            obj.Orders.Add(_1);
                        } while (state.TryReadFieldHeader(1));
                        break;
                    default:
                        state.AppendExtensionData(obj);
                        break;
                }
            }
        }
        private static void Merge(ref ProtoReader.State state, ref protogen.Order obj)
        {
            SubItemToken tok;
            int field;
            if (obj == null) obj = new protogen.Order();
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 1:
                        obj.OrderID = state.ReadInt32();
                        break;
                    case 2:
                        obj.CustomerID = state.ReadString();
                        break;
                    case 3:
                        obj.EmployeeID = state.ReadInt32();
                        break;
                    case 4:
                        obj.OrderDate = BclHelpers.ReadTimestamp(ref state);
                        break;
                    case 5:
                        obj.RequiredDate = BclHelpers.ReadTimestamp(ref state);
                        break;
                    case 6:
                        obj.ShippedDate = BclHelpers.ReadTimestamp(ref state);
                        break;
                    case 7:
                        obj.ShipVia = state.ReadInt32();
                        break;
                    case 8:
                        obj.Freight = state.ReadDouble();
                        break;
                    case 9:
                        obj.ShipName = state.ReadString();
                        break;
                    case 10:
                        obj.ShipAddress = state.ReadString();
                        break;
                    case 11:
                        obj.ShipCity = state.ReadString();
                        break;
                    case 12:
                        obj.ShipRegion = state.ReadString();
                        break;
                    case 13:
                        obj.ShipPostalCode = state.ReadString();
                        break;
                    case 14:
                        obj.ShipCountry = state.ReadString();
                        break;
                    case 15:
                        do
                        {
                            protogen.OrderLine _15 = default;
                            tok = state.StartSubItem();
                            Merge(ref state, ref _15);
                            state.EndSubItem(tok);
                            obj.Lines.Add(_15);
                        } while (state.TryReadFieldHeader(1));
                        break;
                    default:
                        state.AppendExtensionData(obj);
                        break;
                }
            }
        }

        private static void Merge(ref ProtoReader.State state, ref protogen.OrderLine obj)
        {
            int field;
            if (obj == null) obj = new protogen.OrderLine();
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 1:
                        obj.OrderID = state.ReadInt32();
                        break;
                    case 2:
                        obj.ProductID = state.ReadInt32();
                        break;
                    case 3:
                        obj.UnitPrice = state.ReadDouble();
                        break;
                    case 4:
                        state.Hint(WireType.SignedVarint);
                        obj.Quantity = state.ReadInt32();
                        break;
                    case 5:
                        obj.Discount = state.ReadSingle();
                        break;
                    default:
                        state.AppendExtensionData(obj);
                        break;
                }
            }
        }

    }
}
#endif
