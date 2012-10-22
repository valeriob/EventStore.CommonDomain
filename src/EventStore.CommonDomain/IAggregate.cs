using System;
using System.Collections;

namespace CommonDomain
{
	public interface IAggregate
	{
		string Id { get; }
		int Version { get; }

		void ApplyEvent(object @event);
		ICollection GetUncommittedEvents();
		void ClearUncommittedEvents();

		IMemento GetSnapshot();
	}
}