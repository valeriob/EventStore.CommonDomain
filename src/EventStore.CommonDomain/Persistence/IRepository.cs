using System;
using System.Collections.Generic;

namespace CommonDomain.Persistence
{
	public interface IRepository
	{
	    TAggregate GetById<TAggregate>(string id) where TAggregate : class, IAggregate;
		TAggregate GetById<TAggregate>(string id, int version) where TAggregate : class, IAggregate;
		void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders);
	}
}