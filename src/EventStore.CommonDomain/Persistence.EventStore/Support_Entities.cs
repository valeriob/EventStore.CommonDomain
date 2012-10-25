using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.CommonDomain.Persistence
{
    internal class Snapshot
    {
        public object Payload { get; protected set; }
        public virtual string StreamId { get; protected set; }
        public virtual int StreamRevision { get; protected set; }

        public Snapshot(string streamId, int streamRevision, object payload)
        {
            StreamId = streamId;
            StreamRevision = streamRevision;
            Payload = payload;
        }


    }

    internal class Stream
    {
        public string StreamId { get; protected set; }
        public IDictionary<string, object> UncommittedHeaders { get; protected set; }
        public EventMessage[] CommittedEvents { get; protected set; }
        public List<EventMessage> UncommittedEvents { get; protected set; }

        public Stream(string streamId, EventMessage[] committedEvents)
        {
            UncommittedHeaders = new Dictionary<string, object>();
            UncommittedEvents = new List<EventMessage>();
            StreamId = streamId;
            CommittedEvents = committedEvents;
        }

        public void Add(EventMessage uncommittedEvent)
        {
            UncommittedEvents.Add(uncommittedEvent);
        }

        public void ClearChanges()
        {
            UncommittedEvents.Clear();
        }
    }



    public class EventMessage
    {
        public object Body { get; set; }
        public int? EventNumber { get; set; }
    }
}
