using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Nuxleus.WebService {
    public interface IRequest {
        KeyValuePair<string, string>[] Headers { get; }
        RequestType RequestType { get; }
        String RequestMessage { get; set; }
    }
}
