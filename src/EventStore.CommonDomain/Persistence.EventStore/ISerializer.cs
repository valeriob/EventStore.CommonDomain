using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.CommonDomain.Persistence
{
    public interface ISerializer
    {
        EventStore.ClientAPI.IEvent Serialize(EventMessage source);
        EventMessage Deserialize(EventStore.ClientAPI.RecordedEvent source);
    }
}
