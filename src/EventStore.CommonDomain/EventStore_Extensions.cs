using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore;
using CommonDomain.Persistence;
using CommonDomain;

namespace EventStore.CommonDomain
{
    public static class EventStore_Extensions
    {
        public static readonly string Source_Message_Header = "SourceMessage";
        public static void Add_Source_Message_To_Headers(IDictionary<string,object> headers, object sourceMessage)
        {
            headers[Source_Message_Header] = sourceMessage;
        }


        public static void Save(this IRepository repository, IAggregate aggregate, Guid commitId, object sourceMessage)
        {
            repository.Save(aggregate, commitId, (d) => Add_Source_Message_To_Headers(d, sourceMessage));
        }
        public static void Save(this ISagaRepository repository, ISaga saga, Guid commitId, object sourceMessage)
        {
            repository.Save(saga, commitId, (d) => Add_Source_Message_To_Headers(d, sourceMessage) );
        }

    }
}
