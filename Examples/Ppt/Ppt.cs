using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Examples.Ppt
{
    [DataContract]
    public class Test1
    {
        [DataMember(Name="a", Order=1, IsRequired=true)]
        public int A {get;set;}
    }

    [DataContract]
    public class Test3
    {
        [DataMember(Name="c", Order=3, IsRequired=false)]
        public Test1 C {get;set;}
    }

    [ServiceContract]
    public interface ISearchService
    {
        [OperationContract]
        SearchResponse Search(SearchRequest request); 
    }
    
    public class SearchRequest {}
    public class SearchResponse { }
  
}
