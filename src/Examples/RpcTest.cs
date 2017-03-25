//#if DEBUG
//    // Generated from rpc.proto
//    // Option: xml serialization enabled  
    
//      // Option: proto-rpc enabled
    
//    namespace rpc.proto
//    {
      
//    [System.Serializable, ProtoBuf.ProtoContract(Name=@"SearchRequest")]
    
//    [System.Xml.Serialization.XmlType(TypeName=@"SearchRequest")]
    
//    public partial class SearchRequest
//    {
//      public SearchRequest() {}
      
      
//    private string _ID0EU;

//    [ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"query")]
    
//    [System.Xml.Serialization.XmlElementAttribute(@"query", Order = 1)]
    
//    public string query
//    {
//      get { return _ID0EU; }
//      set { _ID0EU = value; }
//    }
  
//    private int _ID0E6 = default(int);

//    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"page_number")]
//    [System.ComponentModel.DefaultValue(default(int))]
    
//    [System.Xml.Serialization.XmlElementAttribute(@"page_number", Order = 2)]
    
//    public int page_number
//    {
//      get { return _ID0E6; }
//      set { _ID0E6 = value; }
//    }
  
//    private int _ID0EIB = default(int);

//    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"result_per_page")]
//    [System.ComponentModel.DefaultValue(default(int))]
    
//    [System.Xml.Serialization.XmlElementAttribute(@"result_per_page", Order = 3)]
    
//    public int result_per_page
//    {
//      get { return _ID0EIB; }
//      set { _ID0EIB = value; }
//    }
  
//    }
  
//    [System.Serializable, ProtoBuf.ProtoContract(Name=@"SearchResponse")]
    
//    [System.Xml.Serialization.XmlType(TypeName=@"SearchResponse")]
    
//    public partial class SearchResponse
//    {
//      public SearchResponse() {}
      
      
//    private readonly System.Collections.Generic.List<string> _ID0EBC = new System.Collections.Generic.List<string>();

//    [ProtoBuf.ProtoMember(1, Name=@"result")]
    
//    [System.Xml.Serialization.XmlElementAttribute(@"result", Order = 1)]
    
//    public System.Collections.Generic.List<string> result
//    {
//      get { return _ID0EBC; }
//      set
//      { // setter needed for XmlSerializer
//        _ID0EBC.Clear();
//        if(value != null)
//        {
//          _ID0EBC.AddRange(value);
//        }
//      }
//    }
  
//    }
  
//    public interface ISearchService
//    {
//      SearchResponse Search(SearchRequest request);
  
//    }
    

//    public class SearchServiceClient : ProtoBuf.ServiceModel.RpcClient
//    {
//      public SearchServiceClient() : base(typeof(ISearchService)) { }

//      SearchResponse Search(SearchRequest request)
//      {
//        return (SearchResponse) Send(@"Search", request);
//      }
  
//    }
    
//    }
//#endif