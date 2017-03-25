using System;
using System.Collections.Generic;

namespace Nuxleus.WebService {
    public interface IRequest {
        KeyValuePair<string, string>[] Headers { get; }
        RequestType RequestType { get; }
        Uri RequestUri { get; set; }
        String RequestMessage { get; set; }
    }
}
