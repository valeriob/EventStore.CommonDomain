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
    public class saga_repository
    {
        [TestMethod]
        public void when_creating_a_saga_repository()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));

            var repository = new EventStore.CommonDomain.Persistence.SagaEventStoreRepository_v1(connection, 
                new My_Serializer(), new Saga_Factory());
        }

        [TestMethod]
        public void when_asking_for_a_saga()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));

            var repository = new EventStore.CommonDomain.Persistence.SagaEventStoreRepository_v1(connection,
                new My_Serializer(), new Saga_Factory());

            var id = Guid.NewGuid() + "";
            var ar = repository.GetById<MySaga>(id);
        }

        [TestMethod]
        public void when_saving_update_saga()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));

            var repository = new EventStore.CommonDomain.Persistence.SagaEventStoreRepository_v1(connection,
               new My_Serializer(), new Saga_Factory());

            var id = Guid.NewGuid() + "";
            var ar = repository.GetById<MySaga>(id);

            ar.Execute_Command(id);
            
            repository.Save(ar, Guid.NewGuid(), null);



            repository = new EventStore.CommonDomain.Persistence.SagaEventStoreRepository_v1(connection,
                new My_Serializer(), new Saga_Factory());

            ar = repository.GetById<MySaga>(id);
        }

    }



}
