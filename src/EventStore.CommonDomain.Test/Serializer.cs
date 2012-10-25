using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.CommonDomain.Persistence;
using Newtonsoft.Json;

namespace EventStore.CommonDomain.Test
{
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
