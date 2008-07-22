using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Nuxleus.WebService {
    public interface IResponse {
        KeyValuePair<string,string>[] Headers { get; set;}
        String Response { get; set; }
    }
}
