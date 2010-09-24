using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using ProtoBuf.Meta;

namespace MonoDto
{
    public static class MyModel
    {
        public static TypeModel CreateSerializer()
        {

            var model = TypeModel.Create();
            model.AutoCompile = false;
            var type = Type.GetType("MonoDto.OrderHeader, MonoDto");
            model.Add(type, true);
            type = Type.GetType("MonoDto.OrderDetail, MonoDto");
            model.Add(type, true);
            return model; //.Compile();
        }
    }
    [ProtoContract]
    public class OrderHeader
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string CustomerRef { get; set; }
        [ProtoMember(3)] public DateTime OrderDate { get; set; }
        [ProtoMember(4)] public DateTime DueDate { get; set; }
        private List<OrderDetail> lines;
        [ProtoMember(5)] public List<OrderDetail> Lines {
            get { return lines ?? (lines = new List<OrderDetail>()); }
        }
    }
    [ProtoContract]
    public class OrderDetail {
        [ProtoMember(1)] public int LineNumber { get; set; }
        [ProtoMember(2)] public string SKU { get; set; }
        [ProtoMember(3)] public int Quantity { get; set; }
        [ProtoMember(4)] public decimal UnitPrice { get; set; }
        [ProtoMember(5)] public string Notes { get; set; }
    }
}
