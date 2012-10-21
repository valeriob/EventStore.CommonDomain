using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace EventStore.CommonDomain.Persistence.CheckPoint
{
    public class MemoryMappedFileCheckpoint : ICheckpoint
    {
        [DllImport("kernel32.dll")]
        static extern bool FlushFileBuffers(IntPtr hFile);

        public string Name
        {
            get { return _name; }
        }

        private readonly string _filename;
        private readonly string _name;
        private readonly bool _cached;
        private readonly MemoryMappedFile _file;
        private long _last;
        private long _lastFlushed;
        private readonly MemoryMappedViewAccessor _accessor;

        public MemoryMappedFileCheckpoint(string filename)
            : this(filename, Guid.NewGuid().ToString(), false)
        {
        }

        public MemoryMappedFileCheckpoint(string filename, string name, bool cached)
        {
            _filename = filename;
            _name = name;
            _cached = cached;
            var filestream = new FileStream(_filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _file = MemoryMappedFile.CreateFromFile(filestream,
                                                    Guid.NewGuid().ToString(),
                                                    8,
                                                    MemoryMappedFileAccess.ReadWrite,
                                                    new MemoryMappedFileSecurity(),
                                                    HandleInheritability.None,
                                                    false);
            _accessor = _file.CreateViewAccessor(0, 8);
            _last = _lastFlushed = ReadCurrent();
        }

        public void Close()
        {
            Flush();
            _accessor.Dispose();
            _file.Dispose();
        }

        public void Write(long checksum)
        {
            Interlocked.Exchange(ref _last, checksum);
        }

        public void Flush()
        {
            _accessor.Write(0, Interlocked.Read(ref _last));
            _accessor.Flush();

            Interlocked.Exchange(ref _lastFlushed, _last);
            FlushFileBuffers(_file.SafeMemoryMappedFileHandle.DangerousGetHandle());
        }

        public long Read()
        {
            return _cached ? Interlocked.Read(ref _lastFlushed) : ReadCurrent();
        }

        private long ReadCurrent()
        {
            return _accessor.ReadInt64(0);
        }

        public long ReadNonFlushed()
        {
            return Interlocked.Read(ref _last);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
