using System;

namespace CommonDomain.Persistence
{
	public interface IConstructAggregates
	{
		IAggregate Build(Type type, string id, IMemento snapshot);
	}
}