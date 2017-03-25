#if !FX11
namespace NETCFClient
{
#if FX30
    [System.Runtime.Serialization.DataContract]
#endif
    public partial class Product
    {
        private int _ProductID;
        private string _ProductName;
        private System.Nullable<int> _SupplierID;
        private System.Nullable<int> _CategoryID;
        private string _QuantityPerUnit;
        private System.Nullable<decimal> _UnitPrice;
        private System.Nullable<short> _UnitsInStock;
        private System.Nullable<short> _UnitsOnOrder;
        private System.Nullable<short> _ReorderLevel;
        private bool _Discontinued;
        private System.Nullable<System.DateTime> _LastEditDate;
        private System.Nullable<System.DateTime> _CreationDate;

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public int ProductID
        {
            get
            {
                return this._ProductID;
            }
            set
            {
                if ((this._ProductID != value))
                {
                    this._ProductID = value;
                }
            }
        }
#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public string ProductName
        {
            get
            {
                return this._ProductName;
            }
            set
            {
                if ((this._ProductName != value))
                {
                    this._ProductName = value;
                }
            }
        }
#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<int> SupplierID
        {
            get
            {
                return this._SupplierID;
            }
            set
            {
                if ((this._SupplierID != value))
                {
                    this._SupplierID = value;
                }
            }
        }
#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<int> CategoryID
        {
            get
            {
                return this._CategoryID;
            }
            set
            {
                if ((this._CategoryID != value))
                {
                    this._CategoryID = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public string QuantityPerUnit
        {
            get
            {
                return this._QuantityPerUnit;
            }
            set
            {
                if ((this._QuantityPerUnit != value))
                {
                    this._QuantityPerUnit = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<decimal> UnitPrice
        {
            get
            {
                return this._UnitPrice;
            }
            set
            {
                if ((this._UnitPrice != value))
                {
                    this._UnitPrice = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<short> UnitsInStock
        {
            get
            {
                return this._UnitsInStock;
            }
            set
            {
                if ((this._UnitsInStock != value))
                {
                    this._UnitsInStock = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<short> UnitsOnOrder
        {
            get
            {
                return this._UnitsOnOrder;
            }
            set
            {
                if ((this._UnitsOnOrder != value))
                {
                    this._UnitsOnOrder = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<short> ReorderLevel
        {
            get
            {
                return this._ReorderLevel;
            }
            set
            {
                if ((this._ReorderLevel != value))
                {
                    this._ReorderLevel = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public bool Discontinued
        {
            get
            {
                return this._Discontinued;
            }
            set
            {
                if ((this._Discontinued != value))
                {
                    this._Discontinued = value;
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<System.DateTime> LastEditDate
        {
            get
            {
                return this._LastEditDate;
            }
            set
            {
                if ((this._LastEditDate != value))
                {
                    this._LastEditDate = value;                    
                }
            }
        }

#if FX30
        [System.Runtime.Serialization.DataMember]
#endif
        public System.Nullable<System.DateTime> CreationDate
        {
            get
            {
                return this._CreationDate;
            }
            set
            {
                if ((this._CreationDate != value))
                {
                    this._CreationDate = value;                    
                }
            }
        }
    }
}
#endif