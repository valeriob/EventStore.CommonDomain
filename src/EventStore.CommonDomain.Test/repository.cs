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
    public class repository
    {
        [TestMethod]
        public void when_creating_a_repository()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));

            var repo = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, new factory(), new ConflictDetector(), null);
        }

        [TestMethod]
        public void when_asking_for_an_aggregate()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
           
            var repo = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, new factory(), new ConflictDetector(), new My_Serializer());

            var id = Guid.NewGuid() + "";
            var ar = repo.GetById<MyAggregate>(id);
        }

        [TestMethod]
        public void when_saving_update_aggregate()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));
       
            var repository = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, new factory(), new ConflictDetector(), new My_Serializer());

            var id = Guid.NewGuid() + "";
            var ar = repository.GetById<MyAggregate>(id);

            ar.Do_Something(id);
            
            repository.Save(ar, Guid.NewGuid(), null);



            repository = new EventStore.CommonDomain.Persistence.EventStoreRepository(connection, new factory(), new ConflictDetector(), new My_Serializer());

            ar = repository.GetById<MyAggregate>(id);
        }

    }

    public interface IEvent
    {
        Guid Id { get; }
    }
    public class factory : IConstructAggregates
    {

        public IAggregate Build(Type type, string id, IMemento snapshot)
        {
            var instance = Activator.CreateInstance(type);
            return instance as IAggregate;
        }
    }

    public class MyAggregate : AggregateBase
    {
        public MyAggregate()
        {
            Register<Done_Something>(e => { Id = e.Ag_Id; });
        }
        public void Do_Something(string id)
        {
            RaiseEvent(new Done_Something 
            { 
                Id = Guid.NewGuid(), 
                Timestamp = DateTime.Now,
                Ag_Id = id,
            });
        }
    }

    public class Done_Something : IEvent
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }

        public string Ag_Id { get; set; }
    }

    public class My_Serializer : ISerializer
    {
        public ClientAPI.IEvent Serialize(EventMessage source)
        {
            var my = source.Body as IEvent;

            string json = JsonConvert.SerializeObject(source.Body);
            var bytes = Encoding.UTF8.GetBytes(json);
            return new InnerEvent 
            { 
                EventId = my.Id, 
                Type = source.Body.GetType().FullName, 
                Data = bytes 
            };
        }

        public EventMessage Deserialize(ClientAPI.RecordedEvent source)
        {
            var json = Encoding.UTF8.GetString(source.Data);
            var type = Type.GetType(source.EventType);
            if (type == null)
                return null;

            var ev = JsonConvert.DeserializeObject(json, type);
            return new EventMessage
            {
                Body = ev, 
                EventNumber = source.EventNumber
            };
        }

        protected class InnerEvent : ClientAPI.IEvent
        {
            public byte[] Data { get; set; }
            public Guid EventId { get; set; }
            public byte[] Metadata { get; set; }
            public string Type { get; set; }
        }
    }
}
