using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Nuxleus.Asynchronous;

namespace Nuxleus.WebService {
    public interface ITask {
        Guid TaskID { get; }
        IRequest Request { get; }
        IResponse Response { get; }
        IEnumerable<IAsync> InvokeAsync();
        IResponse Invoke(ITask task);
    }
}
