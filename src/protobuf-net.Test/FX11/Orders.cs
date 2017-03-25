using System;
using System.Runtime.Serialization;

namespace SampleDto
{
    #if !FX11
    [DataContract]
#endif
    public class OrderHeader
    {
        private int _id;
        #if !FX11
        [DataMember(Order=1)]
#endif
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _customerRef;
        #if !FX11
        [DataMember(Order=2)]
#endif
        public string CustomerRef
        {
            get { return _customerRef; }
            set { _customerRef = value; }
        }

        private DateTime _orderDate;
        #if !FX11
        [DataMember(Order=3)]
#endif
        public DateTime OrderDate
        {
            get { return _orderDate; }
            set { _orderDate = value; }
        }

        private DateTime _dueDate;
        #if !FX11
        [DataMember(Order=4)]
#endif
        public DateTime DueDate
        {
            get { return _dueDate; }
            set { _dueDate = value; }
        }

        private OrderDetail[] lines = new OrderDetail[0];
        #if !FX11
        [DataMember(Order = 5)]
#endif
        public OrderDetail[] Lines { get { return lines; } set { lines = value; } }
    }

    #if !FX11
    [DataContract]
#endif
    public class OrderDetail
    {
        private int _lineNumber;
        #if !FX11
        [DataMember(Order=1)]
#endif
        public int LineNumber
        {
            get { return _lineNumber; }
            set { _lineNumber = value; }
        }

        private string _sku;
        #if !FX11
        [DataMember(Order=2)]
#endif
        public string SKU
        {
            get { return _sku; }
            set { _sku = value; }
        }

        private int _quantity;
        #if !FX11
        [DataMember(Order=3)]
        #endif
        public int Quantity
        {
            get { return _quantity; }
            set { _quantity = value; }
        }

        private decimal _unitPrice;
        #if !FX11
        [DataMember(Order=4)]
        #endif
        public decimal UnitPrice
        {
            get { return _unitPrice; }
            set { _unitPrice = value; }
        }

        private decimal _notes;
        #if !FX11
        [DataMember(Order=5)]
        #endif
        public decimal Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }
    }
}
