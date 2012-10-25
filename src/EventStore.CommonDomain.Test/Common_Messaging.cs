using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonDomain;
using CommonDomain.Core;
using CommonDomain.Persistence;

namespace EventStore.CommonDomain.Test
{
    public interface IEvent
    {
        Guid Id { get; }
    }

    public class Aggregate_Factory : IConstructAggregates
    {

        public IAggregate Build(Type type, string id, IMemento snapshot)
        {
            var instance = Activator.CreateInstance(type);
            return instance as IAggregate;
        }
    }

    public class Saga_Factory : IConstructSagas
    {
        public TSaga Build<TSaga>(string id, IMemento snapshot) where TSaga : ISaga
        {
            var instance = Activator.CreateInstance(typeof(TSaga));
            return (TSaga)instance;
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


    public class MySaga : SagaBase
    {
        Done_Something ev;
        public MySaga()
        {
            Register<Done_Something>(@event =>
                {
                    ev = @event;
                    Id = @event.Ag_Id;
                });
        }


        public void Execute_Command(string id)
        {
            base.Transition(new Done_Something { Id = Guid.NewGuid(), Ag_Id = id, Timestamp = DateTime.Now });
        }
    }
}
