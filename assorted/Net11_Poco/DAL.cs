using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Collections;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
namespace DAL
{
    class Database
    {
        public const DataFormat SubObjectFormat = DataFormat.Group;

        static Database()
        {
            
        }
    }

    [ProtoContract]
    public class VariousFieldTypes
    {
        [ProtoMember(1)] public int Int32;
        [ProtoMember(2)] public uint UInt32;
        [ProtoMember(3)] public long Int64;
        [ProtoMember(4), DefaultValue((ulong)0)] public ulong UInt64;
        [ProtoMember(5)] public short Int16;
        [ProtoMember(6)] public ushort UInt16;
        [ProtoMember(7)] public byte Byte;
        [ProtoMember(8)] public sbyte SByte;
        [ProtoMember(9)] public float Single;
        [ProtoMember(10)] public double Double;
        [ProtoMember(11)] public decimal Decimal;
        [ProtoMember(12)] public string String;
    }

    // these are just so I don't need to hack everything too much
    class TagAttribute : Attribute
    {
        public TagAttribute(int i) { }
    }
    class TableAttribute : Attribute
    {
        public string Name;
    }
    class SerializableAttribute : Attribute
    {
    }
    class ColumnAttribute : Attribute
    {
        public string DbType;
        public string Storage;
        public bool IsDbGenerated;
        public bool IsPrimaryKey;
        public AutoSync AutoSync;
    }
    class AssociationAttribute : Attribute
    {
        public string Storage;
        public string Name;
        public string OtherKey;
    }
    enum AutoSync { OnInsert }

	public class OrderList : CollectionBase
	{
		public int Add(OrderCompat order)
		{
			return List.Add(order);
		}
		public OrderCompat this[int index] 
		{
			get { return (OrderCompat) List[index];}
			set { List[index] = value; }
		}
	}
	public class OrderLineList : CollectionBase
	{
		public int Add(OrderLineCompat orderLine)
		{
			return List.Add(orderLine);
		}
		public OrderLineCompat this[int index] 
		{
			get { return (OrderLineCompat) List[index];}
			set { List[index] = value; }
		}
	}

    [ProtoContract, Serializable]
    public class DatabaseCompat
    {
        public const bool MASTER_GROUP = false;

        [ProtoMember(1, DataFormat = Database.SubObjectFormat), Tag(1)]
        [XmlArray]
        public OrderList Orders;

        public DatabaseCompat()
        {
            Orders = new OrderList();
        }
    }

    [ProtoContract, Serializable]
    public class DatabaseCompatRem
#if REMOTING
    : ISerializable
#endif
#if PLAT_XMLSERIALIZER
    , IXmlSerializable
#endif
    {
        public const bool MASTER_GROUP = false;

        [ProtoMember(1, DataFormat = Database.SubObjectFormat), Tag(1)]
        [XmlArray]
        public OrderList Orders;

        public DatabaseCompatRem()
        {
            Orders = new OrderList();
        }

        #region ISerializable Members
#if REMOTING
    protected DatabaseCompatRem(SerializationInfo info, StreamingContext context)
        : this()
    {
        Serializer.Merge<DatabaseCompatRem>(info, this);
    }
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        Serializer.Serialize <DatabaseCompatRem>(info, this);
    }
#endif
        #endregion

        #region IXmlSerializable Members

#if PLAT_XMLSERIALIZER
    System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
    {
        return null;
    }

    void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
    {
        Serializer.Merge(reader, this);
    }

    void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
    {
        Serializer.Serialize(writer, this);            
    }
#endif
        #endregion
    }

    [ProtoContract, Serializable]
    public class OrderCompat
    {


        private int _OrderID;

        private string _CustomerID;

        private int _EmployeeID;

        private System.DateTime _OrderDate;

        private System.DateTime _RequiredDate;

        private System.DateTime _ShippedDate;

        private int _ShipVia;

        private decimal _Freight;

        private string _ShipName;

        private string _ShipAddress;

        private string _ShipCity;

        private string _ShipRegion;

        private string _ShipPostalCode;

        private string _ShipCountry;

        private OrderLineList _Lines = new OrderLineList();

        public OrderCompat()
        {
            this.Initialize();
        }

        [Column(Storage = "_OrderID", AutoSync = AutoSync.OnInsert, DbType = "Int NOT NULL IDENTITY", IsPrimaryKey = true, IsDbGenerated = true)]
        [ProtoMember(1), Tag(1)]
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
                    //this.OnOrderIDChanging(value);
                    this.SendPropertyChanging();
                    this._OrderID = value;
                    this.SendPropertyChanged("OrderID");
                    //this.OnOrderIDChanged();
                }
            }
        }

        [Column(Storage = "_CustomerID", DbType = "NChar(5)")]
        [ProtoMember(2), Tag(2)]
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
                    //this.OnCustomerIDChanging(value);
                    this.SendPropertyChanging();
                    this._CustomerID = value;
                    this.SendPropertyChanged("CustomerID");
                    //this.OnCustomerIDChanged();
                }
            }
        }

        [Column(Storage = "_EmployeeID", DbType = "Int")]
        [ProtoMember(3), Tag(3)]
        public int EmployeeID
        {
            get
            {
                return this._EmployeeID;
            }
            set
            {
                if ((this._EmployeeID != value))
                {
                    //this.OnEmployeeIDChanging(value);
                    this.SendPropertyChanging();
                    this._EmployeeID = value;
                    this.SendPropertyChanged("EmployeeID");
                    //this.OnEmployeeIDChanged();
                }
            }
        }

        [Column(Storage = "_OrderDate", DbType = "DateTime")]
        [ProtoMember(4), Tag(4)]
        public System.DateTime OrderDate
        {
            get
            {
                return this._OrderDate;
            }
            set
            {
                if ((this._OrderDate != value))
                {
                    //this.OnOrderDateChanging(value);
                    this.SendPropertyChanging();
                    this._OrderDate = value;
                    this.SendPropertyChanged("OrderDate");
                    //this.OnOrderDateChanged();
                }
            }
        }

        [Column(Storage = "_RequiredDate", DbType = "DateTime")]
        [ProtoMember(5), Tag(5)]
        public System.DateTime RequiredDate
        {
            get
            {
                return this._RequiredDate;
            }
            set
            {
                if ((this._RequiredDate != value))
                {
                    // this.OnRequiredDateChanging(value);
                    this.SendPropertyChanging();
                    this._RequiredDate = value;
                    this.SendPropertyChanged("RequiredDate");
                    // this.OnRequiredDateChanged();
                }
            }
        }

        [Column(Storage = "_ShippedDate", DbType = "DateTime")]
        [ProtoMember(6), Tag(6)]
        public System.DateTime ShippedDate
        {
            get
            {
                return this._ShippedDate;
            }
            set
            {
                if ((this._ShippedDate != value))
                {
                    // this.OnShippedDateChanging(value);
                    this.SendPropertyChanging();
                    this._ShippedDate = value;
                    this.SendPropertyChanged("ShippedDate");
                    // this.OnShippedDateChanged();
                }
            }
        }

        [Column(Storage = "_ShipVia", DbType = "Int")]
        [ProtoMember(7), Tag(7)]
        public int ShipVia
        {
            get
            {
                return this._ShipVia;
            }
            set
            {
                if ((this._ShipVia != value))
                {
                    // this.OnShipViaChanging(value);
                    this.SendPropertyChanging();
                    this._ShipVia = value;
                    this.SendPropertyChanged("ShipVia");
                    // this.OnShipViaChanged();
                }
            }
        }

        [Column(Storage = "_Freight", DbType = "Money")]
        [ProtoMember(8), Tag(8)]
        public decimal Freight
        {
            get
            {
                return this._Freight;
            }
            set
            {
                if ((this._Freight != value))
                {
                    // this.OnFreightChanging(value);
                    this.SendPropertyChanging();
                    this._Freight = value;
                    this.SendPropertyChanged("Freight");
                    // this.OnFreightChanged();
                }
            }
        }

        [Column(Storage = "_ShipName", DbType = "NVarChar(40)")]
        [ProtoMember(9), Tag(9)]
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
                    // this.OnShipNameChanging(value);
                    this.SendPropertyChanging();
                    this._ShipName = value;
                    this.SendPropertyChanged("ShipName");
                    // this.OnShipNameChanged();
                }
            }
        }

        [Column(Storage = "_ShipAddress", DbType = "NVarChar(60)")]
        [ProtoMember(10), Tag(10)]
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
                    // this.OnShipAddressChanging(value);
                    this.SendPropertyChanging();
                    this._ShipAddress = value;
                    this.SendPropertyChanged("ShipAddress");
                    // this.OnShipAddressChanged();
                }
            }
        }

        [Column(Storage = "_ShipCity", DbType = "NVarChar(15)")]
        [ProtoMember(11), Tag(11)]
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
                    // this.OnShipCityChanging(value);
                    this.SendPropertyChanging();
                    this._ShipCity = value;
                    this.SendPropertyChanged("ShipCity");
                    // this.OnShipCityChanged();
                }
            }
        }

        [Column(Storage = "_ShipRegion", DbType = "NVarChar(15)")]
        [ProtoMember(12), Tag(12)]
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
                    // this.OnShipRegionChanging(value);
                    this.SendPropertyChanging();
                    this._ShipRegion = value;
                    this.SendPropertyChanged("ShipRegion");
                    // this.OnShipRegionChanged();
                }
            }
        }

        [Column(Storage = "_ShipPostalCode", DbType = "NVarChar(10)")]
        [ProtoMember(13), Tag(13)]
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
                    // this.OnShipPostalCodeChanging(value);
                    this.SendPropertyChanging();
                    this._ShipPostalCode = value;
                    this.SendPropertyChanged("ShipPostalCode");
                    // this.OnShipPostalCodeChanged();
                }
            }
        }

        [Column(Storage = "_ShipCountry", DbType = "NVarChar(15)")]
        [ProtoMember(14), Tag(14)]
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
                    // this.OnShipCountryChanging(value);
                    this.SendPropertyChanging();
                    this._ShipCountry = value;
                    this.SendPropertyChanged("ShipCountry");
                    // this.OnShipCountryChanged();
                }
            }
        }

        [Association(Name = "Order_Order_Detail", Storage = "_Lines", OtherKey = "OrderID")]
        [ProtoMember(15), Tag(15)]
        [XmlArray]
        public OrderLineList Lines
        {
            get
            {
                return this._Lines;
            }
            set { this._Lines = value; }
        }

        protected virtual void SendPropertyChanging()
        {
        }

        protected virtual void SendPropertyChanged(String propertyName)
        {
        }

        private void attach_Lines(OrderLineCompat entity)
        {
            this.SendPropertyChanging();

        }

        private void detach_Lines(OrderLineCompat entity)
        {
            this.SendPropertyChanging();
        }

        private void Initialize()
        {
            // OnCreated();
        }

        [ProtoBeforeDeserialization]
        [System.ComponentModel.EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public void OnDeserializing(StreamingContext context)
        {
            this.Initialize();
        }

        [ProtoBeforeSerialization]
        [System.ComponentModel.EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public void OnSerializing(StreamingContext context)
        {
        }

        [ProtoAfterSerialization]
        [System.ComponentModel.EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public void OnSerialized(StreamingContext context)
        {
        }
    }

    [Table(Name = "dbo.[Order Details]")]
    [ProtoContract, Serializable]
    public class OrderLineCompat
    {

        private int _OrderID;

        private int _ProductID;

        private decimal _UnitPrice;

        private short _Quantity;

        private float _Discount;


        public OrderLineCompat()
        {
            this.Initialize();
        }

        [Column(Storage = "_OrderID", DbType = "Int NOT NULL", IsPrimaryKey = true)]
        [ProtoMember(1), Tag(1)]
        public int OrderID
        {
            get
            {
                return this._OrderID;
            }
            set
            {
                this._OrderID = value;
            }
        }

        [Column(Storage = "_ProductID", DbType = "Int NOT NULL", IsPrimaryKey = true)]
        [ProtoMember(2), Tag(2)]
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
                    // this.OnProductIDChanging(value);
                    this.SendPropertyChanging();
                    this._ProductID = value;
                    this.SendPropertyChanged("ProductID");
                    // this.OnProductIDChanged();
                }
            }
        }

        [Column(Storage = "_UnitPrice", DbType = "Money NOT NULL")]
        [ProtoMember(3), Tag(3)]
        public decimal UnitPrice
        {
            get
            {
                return this._UnitPrice;
            }
            set
            {
                if ((this._UnitPrice != value))
                {
                    // this.OnUnitPriceChanging(value);
                    this.SendPropertyChanging();
                    this._UnitPrice = value;
                    this.SendPropertyChanged("UnitPrice");
                    // this.OnUnitPriceChanged();
                }
            }
        }

        [Column(Storage = "_Quantity", DbType = "SmallInt NOT NULL")]
        [ProtoMember(4), Tag(4)]
        public short Quantity
        {
            get
            {
                return this._Quantity;
            }
            set
            {
                if ((this._Quantity != value))
                {
                    // this.OnQuantityChanging(value);
                    this.SendPropertyChanging();
                    this._Quantity = value;
                    this.SendPropertyChanged("Quantity");
                    // this.OnQuantityChanged();
                }
            }
        }

        [Column(Storage = "_Discount", DbType = "Real NOT NULL")]
        [ProtoMember(5), Tag(5)]
        public float Discount
        {
            get
            {
                return this._Discount;
            }
            set
            {
                if ((this._Discount != value))
                {
                    // this.OnDiscountChanging(value);
                    this.SendPropertyChanging();
                    this._Discount = value;
                    this.SendPropertyChanged("Discount");
                    // this.OnDiscountChanged();
                }
            }
        }


        protected virtual void SendPropertyChanging()
        {

        }

        protected virtual void SendPropertyChanged(String propertyName)
        {

        }

        private void Initialize()
        {
            // OnCreated();
        }

        [ProtoBeforeDeserialization]
        [System.ComponentModel.EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public void OnDeserializing(StreamingContext context)
        {
            this.Initialize();
        }
    }
}