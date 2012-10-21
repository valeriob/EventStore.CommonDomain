using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore.CommonDomain.Persistence.CheckPoint
{
    public interface ICheckpoint : IDisposable
    {
        string Name { get; }
        void Write(long checksum);
        void Flush();
        void Close();

        long Read();
        long ReadNonFlushed();
    }
}
