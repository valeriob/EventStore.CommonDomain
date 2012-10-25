using System;
using System.Collections.Generic;

namespace CommonDomain.Persistence
{
	public interface ISagaRepository
	{
        TSaga GetById<TSaga>(string sagaId) where TSaga : ISaga;
		void Save(ISaga saga, Guid commitId, Action<IDictionary<string, object>> updateHeaders);
	}


    public interface IConstructSagas
    {
        TSaga Build<TSaga>(string id, IMemento snapshot) where TSaga : ISaga;
    }
}