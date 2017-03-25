using System;
using System.Collections.Generic;

namespace Nuxleus.WebService {

    public struct PutObjectRequest : IRequest {

        String m_requestBody;

        #region IRequest Members

        public KeyValuePair<string, string>[] Headers {
            get {
                return
                    new KeyValuePair<string, string>[] {
                        //new KeyValuePair<string,string>("SOAPAction", LabelAttribute.FromMember(RequestType)),
                    };
            }
        }

        public Uri RequestUri { get; set; }

        public RequestType RequestType {
            get {
                return RequestType.PUT;
            }
        }

        public String RequestMessage {
            get {
                return m_requestBody;
            }
            set {
                m_requestBody = value;
            }
        }

        #endregion
    }
}
