using System;
using System.Collections.Generic;
using System.Linq;
using CommonDomain;
using CommonDomain.Persistence;
using EventStore.ClientAPI;

namespace EventStore.CommonDomain.Persistence
{
    /*
    public class Saga_EventStore_Repository_v2 : ISagaRepository, IDisposable
    {
        private const string SagaTypeHeader = "SagaType";
        private const string UndispatchedMessageHeader = "UndispatchedMessage.";
        private readonly IDictionary<string, Stream> _streams = new Dictionary<string, Stream>();
        private readonly EventStoreConnection _eventStoreConnection;
        private readonly ISerializer _serializer;
        private readonly IConstructSagas _sagaFactory;
        private readonly IDictionary<Guid, Snapshot> _snapshots = new Dictionary<Guid, Snapshot>();



        public Saga_EventStore_Repository_v2(EventStoreConnection eventStoreConnection, ISerializer serializer, IConstructSagas sagaFactory)
		{
            _eventStoreConnection = eventStoreConnection;
            _serializer = serializer;
            _sagaFactory = sagaFactory;
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
                _streams.Clear();
            }
        }


        // ********************
        public virtual TSaga GetById<TSaga>(Guid id) where TSaga : class, ISaga
        {
            int versionToLoad = int.MaxValue;

            var snapshot = this.GetSnapshot(id, versionToLoad);
            var stream = this.OpenStream(id, versionToLoad, snapshot);
            var saga = this.GetSaga<TSaga>(id, snapshot, stream);

            foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
                saga.Transition(@event);

            saga.ClearUncommittedEvents();
            saga.ClearUndispatchedMessages();

            return saga as TSaga;
        }
        private ISaga GetSaga<TSaga>(Guid sagaId, Snapshot snapshot, IEventStream stream) where TSaga : class, ISaga
        {
            var memento = snapshot == null ? null : snapshot.Payload as IMemento;
  
            return _sagaFactory.Build<TSaga>(sagaId, memento);
        }
        private IEventStream OpenStream(Guid id, int version, Snapshot snapshot)
        {
            IEventStream stream;
            if (this.streams.TryGetValue(id, out stream))
                return stream;

            stream = snapshot == null
                ? this.eventStore.OpenStream(id, 0, version)
                : this.eventStore.OpenStream(snapshot, version);

            return this.streams[id] = stream;
        }
        private Snapshot GetSnapshot(Guid id, int version)
        {
            Snapshot snapshot;
            if (!this._snapshots.TryGetValue(id, out snapshot))
                this._snapshots[id] = snapshot = this.eventStore.Advanced.GetSnapshot(id, version);

            return snapshot;
        }
        // -----------------------

        private IEventStream OpenStream(Guid sagaId)
        {
            IEventStream stream;
            if (this.streams.TryGetValue(sagaId, out stream))
                return stream;

            try
            {
                stream = this.eventStore.OpenStream(sagaId, 0, int.MaxValue);
            }
            catch (StreamNotFoundException)
            {
                stream = this.eventStore.CreateStream(sagaId);
            }

            return this.streams[sagaId] = stream;
        }

        //private TSaga BuildSaga<TSaga>(Guid sagaId, IEventStream stream) where TSaga : class, ISaga
        //{
        //    var saga = sagaFactory.Build<TSaga>(sagaId);
        //   // var saga = new TSaga();
        //    foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
        //        saga.Transition(@event);

        //    saga.ClearUncommittedEvents();
        //    saga.ClearUndispatchedMessages();

        //    return saga;
        //}

        public void Save(ISaga saga, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
        {
            if (saga == null)
                throw new ArgumentNullException("saga");

            var headers = PrepareHeaders(saga, updateHeaders);
            var stream = this.PrepareStream(saga, headers);

            Persist(stream, commitId);

            saga.ClearUncommittedEvents();
            saga.ClearUndispatchedMessages();
        }
        private static Dictionary<string, object> PrepareHeaders(ISaga saga, Action<IDictionary<string, object>> updateHeaders)
        {
            var headers = new Dictionary<string, object>();

            headers[SagaTypeHeader] = saga.GetType().FullName;
            if (updateHeaders != null)
                updateHeaders(headers);

            var i = 0;
            foreach (var command in saga.GetUndispatchedMessages())
                headers[UndispatchedMessageHeader + i++] = command;

            return headers;
        }
        private IEventStream PrepareStream(ISaga saga, Dictionary<string, object> headers)
        {
            IEventStream stream;
            if (!this.streams.TryGetValue(saga.Id, out stream))
                this.streams[saga.Id] = stream = this.eventStore.CreateStream(saga.Id);

            foreach (var item in headers)
                stream.UncommittedHeaders[item.Key] = item.Value;

            saga.GetUncommittedEvents()
                .Cast<object>()
                .Select(x => new EventMessage { Body = x })
                .ToList()
                .ForEach(stream.Add);

            return stream;
        }
        private static void Persist(IEventStream stream, Guid commitId)
        {
            try
            {
                stream.CommitChanges(commitId);
            }
            catch (DuplicateCommitException)
            {
                stream.ClearChanges();
            }
            catch (EventStore.Persistence.StorageException e)
            {
                throw new CommonDomain.Persistence.PersistenceException(e.Message, e);
            }
        }
    }
     */

    public class SagaEventStoreRepository_v1 : ISagaRepository, IDisposable
	{
		private const string SagaTypeHeader = "SagaType";
		private const string UndispatchedMessageHeader = "UndispatchedMessage.";
        private readonly IDictionary<string, Stream> _streams = new Dictionary<string, Stream>();
        private readonly EventStoreConnection _eventStoreConnection;
        private readonly ISerializer _serializer;
        private readonly IConstructSagas _sagaFactory;

        public SagaEventStoreRepository_v1(EventStoreConnection eventStoreConnection, ISerializer serializer, IConstructSagas sagaFactory)
		{
            _eventStoreConnection = eventStoreConnection;
            _serializer = serializer;
            _sagaFactory = sagaFactory;
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
				_streams.Clear();
			}
		}

        public TSaga GetById<TSaga>(string sagaId) where TSaga : ISaga
		{
			return BuildSaga<TSaga>(OpenStream(sagaId));
		}
        private Stream OpenStream(string sagaId)
		{
            Stream stream;
			if (_streams.TryGetValue(sagaId, out stream))
				return stream;

			try
			{
                _eventStoreConnection.CreateStream(sagaId, new byte[] { });
				//stream = _eventStoreConnection.OpenStream(sagaId, 0, int.MaxValue);
			}
			catch (Exception)
			{
				//stream = this.eventStore.CreateStream(sagaId);
			}

            var slice = _eventStoreConnection.ReadEventStreamForward(sagaId, 0, int.MaxValue);
            var events = slice.Events.Select(s => _serializer.Deserialize(s))
                .Where(e => e != null)
                .ToArray();
            stream = new Stream(sagaId, events);
			return _streams[sagaId] = stream;
		}

        private TSaga BuildSaga<TSaga>(Stream stream) where TSaga : ISaga
		{
            var saga = _sagaFactory.Build<TSaga>(stream.StreamId, null);
			
            foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
				saga.Transition(@event);

			saga.ClearUncommittedEvents();
			saga.ClearUndispatchedMessages();

			return saga;
		}

		public void Save(ISaga saga, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			if (saga == null)
				throw new ArgumentNullException("saga");

			var headers = PrepareHeaders(saga, updateHeaders);
			var stream = PrepareStream(saga, headers);

			Persist(stream, commitId);

			saga.ClearUncommittedEvents();
			saga.ClearUndispatchedMessages();
		}
		private static Dictionary<string, object> PrepareHeaders(ISaga saga, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = new Dictionary<string, object>();

			headers[SagaTypeHeader] = saga.GetType().FullName;
			if (updateHeaders != null)
				updateHeaders(headers);

			var i = 0;
			foreach (var command in saga.GetUndispatchedMessages())
				headers[UndispatchedMessageHeader + i++] = command;

			return headers;
		}
        private Stream PrepareStream(ISaga saga, Dictionary<string, object> headers)
		{
            Stream stream;
            if (!_streams.TryGetValue(saga.Id, out stream))
            {
                System.Diagnostics.Debugger.Break();
                //this.streams[saga.Id] = stream = this.eventStore.CreateStream(saga.Id);
            }
			foreach (var item in headers)
				stream.UncommittedHeaders[item.Key] = item.Value;

			saga.GetUncommittedEvents()
				.Cast<object>()
				.Select(x => new EventMessage { Body = x })
				.ToList()
				.ForEach(stream.Add);

			return stream;
		}
        private void Persist(Stream stream, Guid commitId)
		{
            try
            {
                CommitChanges(stream, commitId);
            }
            //catch (DuplicateCommitException)
            //{
            //    stream.ClearChanges();
            //}
            //catch (StorageException e)
            //{
            //    throw new PersistenceException(e.Message, e);
            //}
            catch (Exception)
            {
                throw;
            }
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