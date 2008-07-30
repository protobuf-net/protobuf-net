using System;
using System.Collections.Generic;
using Nuxleus.Asynchronous;
using SilverlightExtended;

namespace Nuxleus.WebService {
    public interface ITask {
        Guid TaskID { get; }
        IRequest Request { get; }
        IResponse Response { get; }
        int Sequence { get; set; }
        SerializerPerformanceTestAgent Agent { get; set; }
        IEnumerable<IAsync> InvokeAsync();
        IResponse Invoke(ITask task);
    }
}
