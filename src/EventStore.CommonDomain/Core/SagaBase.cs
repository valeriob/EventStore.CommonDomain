using System;
using System.Collections;
using System.Collections.Generic;

namespace CommonDomain.Core
{
    public class SagaBase : ISaga, IEquatable<ISaga>
    {
        private readonly IDictionary<Type, Action<object>> handlers = new Dictionary<Type, Action<object>>();
        private readonly ICollection<object> uncommitted = new LinkedList<object>();
        private readonly ICollection<object> undispatched = new LinkedList<object>();

        public string Id { get; protected set; }
        public int Version { get; private set; }


        protected void Register<TRegisteredMessage>(Action<TRegisteredMessage> handler)
            where TRegisteredMessage : class
        {
            this.handlers[typeof(TRegisteredMessage)] = message => handler(message as TRegisteredMessage);
        }


        public void Transition(object message)
        {
            this.handlers[message.GetType()](message);
            this.uncommitted.Add(message);
            this.Version++;
        }
        ICollection ISaga.GetUncommittedEvents()
        {
            return this.uncommitted as ICollection;
        }
        void ISaga.ClearUncommittedEvents()
        {
            this.uncommitted.Clear();
        }

        protected void Dispatch(object message)
        {
            this.undispatched.Add(message);
        }
        ICollection ISaga.GetUndispatchedMessages()
        {
            return this.undispatched as ICollection;
        }
        void ISaga.ClearUndispatchedMessages()
        {
            this.undispatched.Clear();
        }



        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ISaga);
        }
        public virtual bool Equals(ISaga other)
        {
            return null != other && other.Id == this.Id;
        }
    }
}