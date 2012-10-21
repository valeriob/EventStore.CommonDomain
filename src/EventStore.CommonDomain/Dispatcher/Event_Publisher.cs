using EventStore.ClientAPI;
using EventStore.CommonDomain.Persistence.CheckPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.CommonDomain.Dispatcher
{
    public class Event_Publisher
    {
        EventStoreConnection _eventStoreConnection;
        string _storeId;
        ICheckpoint _checkPoint;

        public Event_Publisher(EventStoreConnection eventStoreConnection, string storeId)
        {
            _eventStoreConnection = eventStoreConnection;
            _storeId = storeId;
        }

        public void Start()
        {
            Init();
        }
        public void Stop()
        { 
        
        }

        private void Init()
        {
            if (_checkPoint == null)
            {

                _checkPoint = new MemoryMappedFileCheckpoint(_storeId + ".chk");
                _checkPoint.Write(0);
                _checkPoint.Flush();
            }

            _eventStoreConnection.SubscribeToAllStreamsAsync(ev => 
            { 
                //TODO publish
                _checkPoint.Write(ev.EventNumber);

            }, () => 
            { 
            
            });
        }
    }
}
