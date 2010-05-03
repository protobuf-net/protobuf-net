using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace SampleDto
{
    [DataContract]
    public class OrderHeader
    {
        [DataMember(Order=1)] public int Id { get; set; }
        [DataMember(Order=2)] public string CustomerRef { get; set; }
        [DataMember(Order=3)] public DateTime OrderDate { get; set; }
        [DataMember(Order=4)] public DateTime DueDate { get; set; }
        private readonly List<OrderDetail> lines = new List<OrderDetail>();
        [DataMember(Order=5)] public List<OrderDetail> Lines { get { return lines; } }
    }
    [DataContract]
    public class OrderDetail
    {
        [DataMember(Order=1)] public int LineNumber { get; set; }
        [DataMember(Order=2)] public string SKU { get; set; }
        [DataMember(Order=3)] public int Quantity { get; set; }
        [DataMember(Order=4)] public decimal UnitPrice { get; set; }
        [DataMember(Order=5)] public decimal Notes { get; set; }
    }
}
