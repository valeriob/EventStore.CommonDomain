using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using CommonDomain.Persistence;
using Newtonsoft.Json;
using System.Text;
using CommonDomain.Core;
using EventStore.CommonDomain.Persistence;
using CommonDomain;

namespace EventStore.CommonDomain.Test
{
    [TestClass]
    public class aggregate_repository
    {
        [TestMethod]
        public void when_creating_an_aggregate_repository()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));

            var repo = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection,
                new Aggregate_Factory(), new ConflictDetector(), new My_Serializer());
        }

        [TestMethod]
        public void when_asking_for_an_aggregate()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
           
            var repo = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, new Aggregate_Factory(), new ConflictDetector(), new My_Serializer());

            var id = Guid.NewGuid() + "";
            var ar = repo.GetById<MyAggregate>(id);
        }

        [TestMethod]
        public void when_saving_update_aggregate()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
       
            var repository = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, 
                new Aggregate_Factory(), new ConflictDetector(), new My_Serializer());

            var id = Guid.NewGuid() + "";
            var ar = repository.GetById<MyAggregate>(id);

            ar.Do_Something(id);
            
            repository.Save(ar, Guid.NewGuid(), null);



            repository = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, 
                new Aggregate_Factory(), new ConflictDetector(), new My_Serializer());

            ar = repository.GetById<MyAggregate>(id);
        }

    }

}
