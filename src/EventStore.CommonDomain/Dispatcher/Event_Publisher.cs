using EventStore.ClientAPI;
using EventStore.CommonDomain.Persistence.CheckPoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;

namespace EventStore.CommonDomain.Dispatcher
{
    public class Event_Publisher
    {
        EventStoreConnection _eventStoreConnection;
        string _storeId;
        string _fileName;
        int _batch;

        MemoryMappedFile _file;
        MemoryMappedViewAccessor _accessor;


        public Event_Publisher(EventStoreConnection eventStoreConnection, string storeId)
        {
            _eventStoreConnection = eventStoreConnection;
            _storeId = storeId;
            _fileName = "checkpoint_" + _storeId;
            _batch = 100;
        }

        public void Start()
        {
            Init();

            Position current;
            _accessor.Read(0, out current);
            var mre = new ManualResetEventSlim(false);
            var _lastRead = Enumerable.Empty<RecordedEvent>();

            _eventStoreConnection.SubscribeToAllStreamsAsync((ev, pos) =>
            {
                if (!mre.IsSet)
                    mre.Wait();

                _accessor.Read(0, out current);

                if (current.Commit > pos.CommitPosition)
                    return;

                Publish(new[] { ev });
            }, () =>
            {
                // dropped, refresh
            });

            
            _eventStoreConnection.ReadAllEventsForwardAsync(new ClientAPI.Position(current.Commit, current.Prepare), _batch)
              .ContinueWith(s =>
              {
                  Publish(s.Result.Events);

                  _lastRead = s.Result.Events;

                  int lastCount = current.Count;
                  current = new Position
                  {
                      Prepare = s.Result.Position.PreparePosition,
                      Commit = s.Result.Position.CommitPosition,
                      Count = lastCount + s.Result.Events.Count()
                  };
                  _accessor.Write(0, ref current);

                  mre.Set();
              }).Wait();
        }
        public void Stop()
        { 
        
        }
        public void Reset()
        {
            File.Delete(_fileName);
        }

        private void Init()
        {
            if (!File.Exists(_fileName))
            {
                _file = MemoryMappedFile.CreateNew(_fileName, 4096);
                _accessor = _file.CreateViewAccessor();
                var begin = new Position();
                _accessor.Write(0, ref begin);
            }
            else
            {
                _file = MemoryMappedFile.CreateOrOpen(_fileName, 4096);
                _accessor = _file.CreateViewAccessor();
            }

            while (true)
            { 
                Position current;
                _accessor.Read(0, out current);

                var _lastRead = Enumerable.Empty<RecordedEvent>();

                _eventStoreConnection.ReadAllEventsForwardAsync(new ClientAPI.Position(current.Commit, current.Prepare), _batch)
                    .ContinueWith(s =>
                    {
                        Publish(s.Result.Events);

                        _lastRead = s.Result.Events;

                        int lastCount = current.Count;
                        current = new Position
                        {
                            Prepare = s.Result.Position.PreparePosition,
                            Commit = s.Result.Position.CommitPosition,
                            Count = lastCount + s.Result.Events.Count()
                        };
                        _accessor.Write(0, ref current);
                    }).Wait();

                if (_lastRead.Count() < _batch)
                    break;
            }

        }

        /// <summary>
        /// Best to do it in transaction if possible
        /// </summary>
        /// <param name="events"></param>
        private void Publish(IEnumerable<RecordedEvent> events)
        { 
            foreach(var ev in events)
                Console.WriteLine("{0} : {1}, {2}", ev.EventNumber, ev.EventType, ev.EventStreamId);
        }

        protected struct Position
        {
            public long Commit;
            public long Prepare;
            public int Count;
        }
    }
}
