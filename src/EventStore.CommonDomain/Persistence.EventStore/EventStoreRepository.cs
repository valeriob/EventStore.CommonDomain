using EventStore.ClientAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EventStore.CommonDomain.Persistence
{
    public interface ISerializer
    {
        EventStore.ClientAPI.IEvent Serialize(EventMessage source);
        EventMessage Deserialize(EventStore.ClientAPI.RecordedEvent source);
    }


	public class EventStoreRepository :  IDisposable
	{
		private const string AggregateTypeHeader = "AggregateType";
        private readonly IDictionary<string, Snapshot> snapshots = new Dictionary<string, Snapshot>();
        private readonly IDictionary<string, Stream> streams = new Dictionary<string, Stream>();
        private readonly EventStoreConnection eventStoreConnection;
		private readonly IConstructAggregates factory;
		private readonly IDetectConflicts conflictDetector;

        private readonly ISerializer _serializer;

		public EventStoreRepository(
            EventStoreConnection eventStoreConnection,
			IConstructAggregates factory,
			IDetectConflicts conflictDetector, ISerializer serializer)
		{
			this.eventStoreConnection = eventStoreConnection;
			this.factory = factory;
			this.conflictDetector = conflictDetector;
            this._serializer = serializer;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			lock (this.streams)
			{
                //foreach (var stream in this.streams)
                //    stream.Value.Dispose();

				this.snapshots.Clear();
				this.streams.Clear();
			}
		}

        public virtual TAggregate GetById<TAggregate>(string id) where TAggregate : class, IAggregate
        {
            return GetById<TAggregate>(id, int.MaxValue);
        }

        public virtual TAggregate GetById<TAggregate>(string id, int versionToLoad) where TAggregate : class, IAggregate
		{
			var snapshot = this.GetSnapshot(id, versionToLoad);
			var stream = this.OpenStream(id, versionToLoad, snapshot);
			var aggregate = this.GetAggregate<TAggregate>(snapshot, stream);

			ApplyEventsToAggregate(versionToLoad, stream, aggregate);

			return aggregate as TAggregate;
		}
        private static void ApplyEventsToAggregate(int versionToLoad, Stream stream, IAggregate aggregate)
		{
			if (versionToLoad == 0 || aggregate.Version < versionToLoad)
				foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
					aggregate.ApplyEvent(@event);
		}
        private IAggregate GetAggregate<TAggregate>(Snapshot snapshot, Stream stream)
		{
			var memento = snapshot == null ? null : snapshot.Payload as IMemento;
			return this.factory.Build(typeof(TAggregate), stream.StreamId, memento);
		}
		private Snapshot GetSnapshot(string id, int version)
		{
			Snapshot snapshot;
            if (!this.snapshots.TryGetValue(id, out snapshot))
            {
                try
                {
                    var stream = this.eventStoreConnection.ReadEventStreamBackward(id, int.MaxValue, 1);
                    // TODO : Optimization required
                    var last = stream.Events.LastOrDefault();

                    this.snapshots[id] = snapshot = new Snapshot(id, last.EventNumber, _serializer.Deserialize(last));
                }
                catch (Exception) { }
            }
				
			return snapshot;
		}
        private Stream OpenStream(string id, int version, Snapshot snapshot)
		{
            Stream stream;
			if (this.streams.TryGetValue(id, out stream))
				return stream;

            var minRevision = 0;
            if (snapshot != null)
                minRevision = snapshot.StreamRevision;
            try
            {
                eventStoreConnection.CreateStream(id, new byte [] { });
            }
            catch (Exception) { } // TODO catch stream exists or verify it with a to be command

            var esStream = eventStoreConnection.ReadEventStreamForward(id, minRevision, version);
            stream = new Stream(esStream.Stream, esStream.Events.Select(e => _serializer.Deserialize(e))
                .Where(e => e!=null)
                .ToArray());

            return streams[id] = stream;
		}

		public virtual void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = PrepareHeaders(aggregate, updateHeaders);
			while (true)
			{
				var stream = this.PrepareStream(aggregate, headers);
				var commitEventCount = stream.CommittedEvents.Length;

				try
				{
                    CommitChanges(stream, commitId);
					//stream.CommitChanges(commitId);
					aggregate.ClearUncommittedEvents();
					return;
				}
                    // TODO expected version 

                //catch (DuplicateCommitException)
                //{
                //    stream.ClearChanges();
                //    return;
                //}
                //catch (ConcurrencyException e)
                //{
                //    if (this.ThrowOnConflict(stream, commitEventCount))
                //        throw new ConflictingCommandException(e.Message, e);

                //    stream.ClearChanges();
                //}
                //catch (StorageException e)
                //{
                //    throw new PersistenceException(e.Message, e);
                //}

                catch (Exception e)
                {
                    throw new PersistenceException(e.Message, e);
                }
			}
		}
        private Stream PrepareStream(IAggregate aggregate, Dictionary<string, object> headers)
		{
            Stream stream;
            if (!this.streams.TryGetValue(aggregate.Id, out stream))
            {
                Debugger.Break();

                this.eventStoreConnection.CreateStream(aggregate.Id, new byte[] {});
                this.streams[aggregate.Id] = stream = new Stream(aggregate.Id, new EventMessage[0]);
            }

			foreach (var item in headers)
				stream.UncommittedHeaders[item.Key] = item.Value;

            aggregate.GetUncommittedEvents()
                .Cast<object>()
                .Select(x => new EventMessage { Body = x })
                .ToList()
                .ForEach(stream.Add);

			return stream;
		}
		private static Dictionary<string, object> PrepareHeaders(IAggregate aggregate, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = new Dictionary<string, object>();

			headers[AggregateTypeHeader] = aggregate.GetType().FullName;
			if (updateHeaders != null)
				updateHeaders(headers);

			return headers;
		}
        private bool ThrowOnConflict(Stream stream, int skip)
		{
			var committed = stream.CommittedEvents.Skip(skip).Select(x => x.Body);
			var uncommitted = stream.UncommittedEvents.Select(x => x.Body);
			return this.conflictDetector.ConflictsWith(uncommitted, committed);
		}


        private void CommitChanges(Stream stream, Guid commitId)
        {
            int expectedVersion = stream.CommittedEvents.Select(e => e.EventNumber.Value)
                .DefaultIfEmpty().Max();

            var events = stream.UncommittedEvents.Select(e => _serializer.Serialize(e));

            eventStoreConnection.AppendToStream(stream.StreamId + "", expectedVersion, events);
        }
	}

  
    public class Snapshot
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

    public class Stream
    {
        public string StreamId { get; protected set;  }
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