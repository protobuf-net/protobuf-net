#if !FX11
namespace NETCFClient
{
    public partial class Order
    {
        private int _OrderID;

        private string _CustomerID;

        private System.Nullable<int> _EmployeeID;

        private System.Nullable<System.DateTime> _OrderDate;

        private System.Nullable<System.DateTime> _RequiredDate;

        private System.Nullable<System.DateTime> _ShippedDate;

        private System.Nullable<int> _ShipVia;

        private System.Nullable<decimal> _Freight;

        private string _ShipName;

        private string _ShipAddress;

        private string _ShipCity;

        private string _ShipRegion;

        private string _ShipPostalCode;

        private string _ShipCountry;

        
        public int OrderID
        {
            get
            {
                return this._OrderID;
            }
            set
            {
                if ((this._OrderID != value))
                {
                    this._OrderID = value;
                }
            }
        }

        public string CustomerID
        {
            get
            {
                return this._CustomerID;
            }
            set
            {
                if ((this._CustomerID != value))
                {
                    this._CustomerID = value;
                }
            }
        }

        public System.Nullable<int> EmployeeID
        {
            get
            {
                return this._EmployeeID;
            }
            set
            {
                if ((this._EmployeeID != value))
                {
                    this._EmployeeID = value;
                }
            }
        }

        public System.Nullable<System.DateTime> OrderDate
        {
            get
            {
                return this._OrderDate;
            }
            set
            {
                if ((this._OrderDate != value))
                {
                    this._OrderDate = value;
                }
            }
        }

        public System.Nullable<System.DateTime> RequiredDate
        {
            get
            {
                return this._RequiredDate;
            }
            set
            {
                if ((this._RequiredDate != value))
                {
                    this._RequiredDate = value;
                }
            }
        }

        public System.Nullable<System.DateTime> ShippedDate
        {
            get
            {
                return this._ShippedDate;
            }
            set
            {
                if ((this._ShippedDate != value))
                {
                    this._ShippedDate = value;
                }
            }
        }

        public System.Nullable<int> ShipVia
        {
            get
            {
                return this._ShipVia;
            }
            set
            {
                if ((this._ShipVia != value))
                {
                    this._ShipVia = value;
                }
            }
        }

        public System.Nullable<decimal> Freight
        {
            get
            {
                return this._Freight;
            }
            set
            {
                if ((this._Freight != value))
                {
                    this._Freight = value;
                }
            }
        }

        public string ShipName
        {
            get
            {
                return this._ShipName;
            }
            set
            {
                if ((this._ShipName != value))
                {
                    this._ShipName = value;
                }
            }
        }

        public string ShipAddress
        {
            get
            {
                return this._ShipAddress;
            }
            set
            {
                if ((this._ShipAddress != value))
                {
                    this._ShipAddress = value;
                }
            }
        }

        public string ShipCity
        {
            get
            {
                return this._ShipCity;
            }
            set
            {
                if ((this._ShipCity != value))
                {
                    this._ShipCity = value;
                }
            }
        }

        public string ShipRegion
        {
            get
            {
                return this._ShipRegion;
            }
            set
            {
                if ((this._ShipRegion != value))
                {
                    this._ShipRegion = value;
                }
            }
        }

        public string ShipPostalCode
        {
            get
            {
                return this._ShipPostalCode;
            }
            set
            {
                if ((this._ShipPostalCode != value))
                {
                    this._ShipPostalCode = value;
                }
            }
        }

        public string ShipCountry
        {
            get
            {
                return this._ShipCountry;
            }
            set
            {
                if ((this._ShipCountry != value))
                {
                    this._ShipCountry = value;
                }
            }
        }
    }
}
#endif