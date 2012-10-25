using EventStore.ClientAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommonDomain.Persistence;
using CommonDomain;

namespace EventStore.CommonDomain.Persistence
{
	public class EventStoreRepository :  IRepository, IDisposable
	{
		private const string AggregateTypeHeader = "AggregateType";
        private readonly IDictionary<string, Snapshot> _snapshots = new Dictionary<string, Snapshot>();
        private readonly IDictionary<string, Stream> _streams = new Dictionary<string, Stream>();
        private readonly EventStoreConnection _eventStoreConnection;
		private readonly IConstructAggregates _factory;
		private readonly IDetectConflicts _conflictDetector;

        private readonly ISerializer _serializer;

		public EventStoreRepository(
            EventStoreConnection eventStoreConnection,
			IConstructAggregates factory,
			IDetectConflicts conflictDetector, ISerializer serializer)
		{
			_eventStoreConnection = eventStoreConnection;
			_factory = factory;
			_conflictDetector = conflictDetector;
            _serializer = serializer;
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

			lock (_streams)
			{
                //foreach (var stream in this.streams)
                //    stream.Value.Dispose();

				_snapshots.Clear();
				_streams.Clear();
			}
		}

        public virtual TAggregate GetById<TAggregate>(string id) where TAggregate : class, IAggregate
        {
            return GetById<TAggregate>(id, int.MaxValue);
        }

        public virtual TAggregate GetById<TAggregate>(string id, int versionToLoad) where TAggregate : class, IAggregate
		{
			var snapshot = GetSnapshot(id, versionToLoad);
			var stream = OpenStream(id, versionToLoad, snapshot);
			var aggregate = GetAggregate<TAggregate>(snapshot, stream);

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
			return _factory.Build(typeof(TAggregate), stream.StreamId, memento);
		}
		private Snapshot GetSnapshot(string id, int version)
		{
			Snapshot snapshot;
            if (!_snapshots.TryGetValue(id, out snapshot))
            {
                try
                {
                    var stream = _eventStoreConnection.ReadEventStreamBackward(id, int.MaxValue, 1);
                    // TODO : Optimization required
                    var last = stream.Events.LastOrDefault();

                    _snapshots[id] = snapshot = new Snapshot(id, last.EventNumber, _serializer.Deserialize(last));
                }
                catch (Exception) { }
            }
				
			return snapshot;
		}
        private Stream OpenStream(string id, int version, Snapshot snapshot)
		{
            Stream stream;
			if (_streams.TryGetValue(id, out stream))
				return stream;

            var minRevision = 0;
            if (snapshot != null)
                minRevision = snapshot.StreamRevision;
            try
            {
                _eventStoreConnection.CreateStream(id, new byte [] { });
            }
            catch (Exception) { } // TODO catch stream exists or verify it with a to be command

            var esStream = _eventStoreConnection.ReadEventStreamForward(id, minRevision, version);
            stream = new Stream(esStream.Stream, esStream.Events.Select(e => _serializer.Deserialize(e))
                .Where(e => e!=null)
                .ToArray());

            return _streams[id] = stream;
		}

		public virtual void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = PrepareHeaders(aggregate, updateHeaders);
			while (true)
			{
				var stream = PrepareStream(aggregate, headers);
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
            if (!_streams.TryGetValue(aggregate.Id, out stream))
            {
                Debugger.Break();

                _eventStoreConnection.CreateStream(aggregate.Id, new byte[] {});
                _streams[aggregate.Id] = stream = new Stream(aggregate.Id, new EventMessage[0]);
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
			return _conflictDetector.ConflictsWith(uncommitted, committed);
		}


        private void CommitChanges(Stream stream, Guid commitId)
        {
            int expectedVersion = stream.CommittedEvents.Select(e => e.EventNumber.Value)
                .DefaultIfEmpty().Max();

            var events = stream.UncommittedEvents.Select(e => _serializer.Serialize(e));
            
            _eventStoreConnection.AppendToStream(stream.StreamId + "", expectedVersion, events);
        }
	}

 
}