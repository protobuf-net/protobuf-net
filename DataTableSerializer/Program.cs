using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using ProtoBuf;

namespace ExperimentalDataTableSerialization
{
    class Program
    {
        static DataTable LoadDataTableFromDatabase()
        {
            DataTable table = new DataTable("Sales.SalesOrderDetail");
            using (var conn = new SqlConnection("Data Source=.;Initial Catalog=AdventureWorks2008R2;Integrated Security=True"))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from Sales.SalesOrderDetail";
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    table.Load(reader);
                }
            }
            return table;
        }
        static void ExecuteWithTiming(string caption, Action action)
        {
            CleanupObjectsBeforeTiming();
            var watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            Console.WriteLine("{0}\t{1}ms", caption, watch.ElapsedMilliseconds);
        }
        static T ExecuteWithTiming<T>(string caption, Func<T> func)
        {
            CleanupObjectsBeforeTiming();
            var watch = Stopwatch.StartNew();
            T value = func();
            watch.Stop();
            Console.WriteLine("{0}\t{1}ms", caption, watch.ElapsedMilliseconds);
            return value;
        }
        static void Main()
        {
            var table = ExecuteWithTiming<DataTable>("Load table", LoadDataTableFromDatabase);
            Console.WriteLine("Table loaded\t{0} cols\t{1} rows", table.Columns.Count, table.Rows.Count);
            BinaryFormatter bf = new BinaryFormatter();

            WriteWithTiming("DataTable (xml)", stream => table.WriteXml(stream, XmlWriteMode.WriteSchema), stream =>
            {
                var dt = new DataTable();
                dt.ReadXml(stream);
                CheckTables(table, dt);
            });
            table.RemotingFormat = SerializationFormat.Xml;
            WriteWithTiming("BinaryFormatter (rf:xml)", stream => bf.Serialize(stream, table), stream => CheckTables(table, bf.Deserialize(stream)));
            table.RemotingFormat = SerializationFormat.Binary;
            WriteWithTiming("BinaryFormatter (rf:binary)", stream => bf.Serialize(stream, table), stream => CheckTables(table, bf.Deserialize(stream)));

            WriteWithTiming("protobuf-net v2", stream => ProtoWrite(table, stream), stream => CheckTables(table, ProtoRead(stream)));
        }
        static void CleanupObjectsBeforeTiming()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
        static DataTable ProtoRead(Stream stream)
        {
            DataTable table = new DataTable();
            object[] values = null;
            using (ProtoReader reader = new ProtoReader(stream, null))
            {
                int field;
                List<Func<object>> colReaders = new List<Func<object>>();
                SubItemToken token;
                while ((field = reader.ReadFieldHeader()) != 0)
                {
                    switch (field)
                    {
                        case 1:
                            table.TableName = reader.ReadString();
                            break;
                        case 2:
                            string name = null;
                            MappedType mappedType = (MappedType)(-1);
                            token = ProtoReader.StartSubItem(reader);
                            while ((field = reader.ReadFieldHeader()) != 0)
                            {
                                switch (field)
                                {
                                    case 1:
                                        name = reader.ReadString();
                                        break;
                                    case 2:
                                        mappedType = (MappedType)reader.ReadInt32();
                                        break;
                                    default:
                                        reader.SkipField();
                                        break;
                                }
                            }
                            Type type;
                            switch (mappedType)
                            {
                                case MappedType.Int32:
                                    type = typeof(int);
                                    colReaders.Add(() => reader.ReadInt32());
                                    break;
                                case MappedType.Int16:
                                    type = typeof(short);
                                    colReaders.Add(() => reader.ReadInt16());
                                    break;
                                case MappedType.Decimal:
                                    type = typeof(decimal);
                                    colReaders.Add(() => BclHelpers.ReadDecimal(reader));
                                    break;
                                case MappedType.String:
                                    type = typeof(string);
                                    colReaders.Add(() => reader.ReadString());
                                    break;
                                case MappedType.Guid:
                                    type = typeof(Guid);
                                    colReaders.Add(() => BclHelpers.ReadGuid(reader));
                                    break;
                                case MappedType.DateTime:
                                    type = typeof(DateTime);
                                    colReaders.Add(() => BclHelpers.ReadDateTime(reader));
                                    break;
                                default:
                                    throw new NotSupportedException(mappedType.ToString());
                            }
                            ProtoReader.EndSubItem(token, reader);
                            table.Columns.Add(name, type);
                            values = null;
                            break;
                        case 3:
                            if (values == null) values = new object[table.Columns.Count];
                            else Array.Clear(values, 0, values.Length);
                            token = ProtoReader.StartSubItem(reader);
                            while ((field = reader.ReadFieldHeader()) != 0)
                            {
                                if (field > values.Length) reader.SkipField();
                                else
                                {
                                    int i = field - 1;
                                    values[i] = colReaders[i]();
                                }
                            }
                            ProtoReader.EndSubItem(token, reader);
                            table.Rows.Add(values);
                            break;
                        default:
                            reader.SkipField();
                            break;
                    }
                }
            }
            return table;
        }
        static void CheckTables(DataTable x, object other)
        {
            DataTable y = (DataTable)other;
            if (x.TableName != y.TableName) Console.WriteLine("names do not match");
            if (x.Columns.Count != y.Columns.Count) Console.WriteLine("columns do not match");
            if (x.Rows.Count != y.Rows.Count) Console.WriteLine("rows do not match");
        }
        enum MappedType
        {
            Int16, Int32, String, Decimal, Guid, DateTime
        }
        static void ProtoWrite(DataTable table, Stream stream)
        {
            using (var writer = new ProtoWriter(stream, null))
            {
                // table name
                if (!string.IsNullOrEmpty(table.TableName))
                {
                    ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                    ProtoWriter.WriteString(table.TableName, writer);
                }

                // write the schema:
                var cols = table.Columns;
                Action<object>[] colWriters = new Action<object>[cols.Count];
                int i = 0;

                foreach (DataColumn col in cols)
                {
                    // for each, write the name and data type
                    ProtoWriter.WriteFieldHeader(2, WireType.StartGroup, writer);
                    var token = ProtoWriter.StartSubItem(col, writer);
                    ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                    ProtoWriter.WriteString(col.ColumnName, writer);
                    ProtoWriter.WriteFieldHeader(2, WireType.Variant, writer);
                    MappedType type;
                    switch (Type.GetTypeCode(col.DataType))
                    {
                        case TypeCode.Decimal: type = MappedType.Decimal; break;
                        case TypeCode.Int16: type = MappedType.Int16; break;
                        case TypeCode.Int32: type = MappedType.Int32; break;
                        case TypeCode.String: type = MappedType.String; break;
                        case TypeCode.DateTime: type = MappedType.DateTime; break;
                        default:
                            if (col.DataType == typeof(Guid))
                            {
                                type = MappedType.Guid; break;
                            }
                            throw new NotSupportedException(col.DataType.Name);
                    }
                    ProtoWriter.WriteInt32((int)type, writer);
                    ProtoWriter.EndSubItem(token, writer);
                    int field = i + 1;
                    Action<object> colWriter;
                    switch (type)
                    {
                        case MappedType.String:
                            colWriter = value =>
                            {
                                ProtoWriter.WriteFieldHeader(field, WireType.String, writer);
                                ProtoWriter.WriteString((string)value, writer);
                            };
                            break;
                        case MappedType.Int16:
                            colWriter = value =>
                            {
                                ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                ProtoWriter.WriteInt16((short)value, writer);
                            };
                            break;
                        case MappedType.Decimal:
                            colWriter = value =>
                            {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                BclHelpers.WriteDecimal((decimal)value, writer);
                            };
                            break;
                        case MappedType.Int32:
                            colWriter = value =>
                            {
                                ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                ProtoWriter.WriteInt32((int)value, writer);
                            };
                            break;
                        case MappedType.Guid:
                            colWriter = value =>
                            {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                BclHelpers.WriteGuid((Guid)value, writer);
                            };
                            break;
                        case MappedType.DateTime:
                            colWriter = value =>
                            {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                BclHelpers.WriteDateTime((DateTime)value, writer);
                            };
                            break;
                        default:
                            throw new NotSupportedException(col.DataType.Name);
                    }
                    colWriters[i++] = colWriter;
                }
                // write the rows
                foreach (DataRow row in table.Rows)
                {
                    i = 0;
                    ProtoWriter.WriteFieldHeader(3, WireType.StartGroup, writer);
                    var token = ProtoWriter.StartSubItem(row, writer);
                    foreach (DataColumn col in cols)
                    {
                        var value = row[col];
                        if (value == null || value is DBNull) { }
                        else { colWriters[i](value); }
                        i++;
                    }
                    ProtoWriter.EndSubItem(token, writer);
                }
            }
        }
        static void WriteWithTiming(string caption, Action<Stream> serialize, Action<Stream> deserialize)
        {

            using (var ms = new MemoryStream())
            {
                CleanupObjectsBeforeTiming();
                var watch1 = Stopwatch.StartNew();
                serialize(ms);
                watch1.Stop();
                ms.Position = 0;
                CleanupObjectsBeforeTiming();
                var watch2 = Stopwatch.StartNew();
                deserialize(ms);
                watch2.Stop();
                Console.WriteLine("{0} (vanilla)\t{1}ms/{2}ms\t{3:###,###} bytes", caption, watch1.ElapsedMilliseconds, watch2.ElapsedMilliseconds, ms.Length);


            }
            using (var ms = new MemoryStream())
            {
                Stopwatch watch1, watch2;
                using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    CleanupObjectsBeforeTiming();
                    watch1 = Stopwatch.StartNew();
                    serialize(gzip);
                    watch1.Stop();
                    gzip.Close();
                }
                ms.Position = 0;
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    CleanupObjectsBeforeTiming();
                    watch2 = Stopwatch.StartNew();
                    deserialize(gzip);
                    watch2.Stop();
                    gzip.Close();
                }
                Console.WriteLine("{0} (gzip)\t{1}ms/{2}ms\t{3:###,###} bytes", caption, watch1.ElapsedMilliseconds, watch2.ElapsedMilliseconds, ms.Length);
            }
            using (var ms = new MemoryStream())
            {
                Stopwatch watch1, watch2;
                using (var deflate = new DeflateStream(ms, CompressionMode.Compress, true))
                {
                    CleanupObjectsBeforeTiming();
                    watch1 = Stopwatch.StartNew();
                    serialize(deflate);
                    watch1.Stop();
                    deflate.Close();
                }
                ms.Position = 0;
                using (var deflate = new DeflateStream(ms, CompressionMode.Decompress, true))
                {
                    CleanupObjectsBeforeTiming();
                    watch2 = Stopwatch.StartNew();
                    deserialize(deflate);
                    watch2.Stop();
                    deflate.Close();
                }
                Console.WriteLine("{0} (deflate)\t{1}ms/{2}ms\t{3:###,###} bytes", caption, watch1.ElapsedMilliseconds, watch2.ElapsedMilliseconds, ms.Length);
            }

        }

    }
}