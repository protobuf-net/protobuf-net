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
        //[Benchmark(Baseline = true)]
        //[Benchmark]
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
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }

        internal void Verify(byte[] data, int length)
        {
            if(_data.Length != length)
                throw new InvalidOperationException($"Length mismatch; {_data.Length} vs {length}");
            for (int i = 0; i < _data.Length;i++)
            {
                if (_data[i] != data[i])
                    throw new InvalidOperationException($"Data mismatch at offset {i}; {Convert.ToString(_data[i], 16)} vs {Convert.ToString(data[i], 16)}");
            }
            Console.WriteLine($"verified: {length} bytes");
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

        //[Benchmark(Description = "MemoryStream")]
        [Benchmark(Description = "MemoryStream", Baseline = true)]
        public protogen.Database MemoryStream()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(out var state, Exposable(_data), model))
            {
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }

        private static MemoryStream Exposable(byte[] data)
            => new MemoryStream(data, 0, data.Length, false, true);

        [Benchmark(Description = "MemoryStream*")]
        public protogen.Database MemoryStream_Manual()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(out var state, Exposable(_data), model))
            {
                protogen.Database obj = default;
                Merge(reader, ref state, ref obj);
                return obj;
            }
        }

        [Benchmark(Description = "ROM_CIP")]
        public protogen.Database ROM_CIP()
        {
            var model = _cip;
            using (var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }

        [Benchmark(Description = "ROM_C")]
        public protogen.Database ROM_C()
        {
            var model = _c;
            using (var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }


        [Benchmark(Description = "ROM_DLL")]
        public protogen.Database ROM_DLL()
        {
#if (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP3_0)
            throw new NotSupportedException();
#else
            var model = _dll;
            using (var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
#endif
        }

        [Benchmark(Description = "ROM_AUTO")]
        public protogen.Database ROM_AUTO()
        {
            var model = _auto;
            using (var reader = ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }

        [Benchmark(Description = "MemoryStream_AUTO")]
        public protogen.Database MemoryStream_AUTO()
        {
            var model = _auto;
            using (var reader = ProtoReader.Create(out var state, new MemoryStream(_data), model))
            {
                return (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }

        public TypeModel Auto => _auto;

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

        [GlobalSetup]
        public void Setup()
        {
            _data = File.ReadAllBytes("test.bin");
            var model = TypeModel.Create();
            model.Add(typeof(protogen.Database), true);
            model.Add(typeof(protogen.Order), true);
            model.Add(typeof(protogen.OrderLine), true);
            model.CompileInPlace();
            _cip = model;
            _c = model.Compile();
#if !(NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP3_0)
            _dll = model.Compile("MySerializer", "DalSerializer.dll");
#endif
            _auto = TypeModel.CreateForAssembly<protogen.Database>();
        }

#pragma warning disable CS0649
        TypeModel _cip, _c, _auto;
#if !(NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP3_0)
        TypeModel _dll;
#endif
#pragma warning restore CS0649
    }
}
