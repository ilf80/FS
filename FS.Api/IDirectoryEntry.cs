using System;
using System.Collections.Generic;

namespace FS
{
    public interface IDirectoryEntry : IDisposable
    {
        IEnumerable<IFileSystemEntry> GetEntries();

        bool TryGetEntry(string name, out IFileSystemEntry entry);

        IFileEntry OpenFile(string name, OpenMode mode);

        void DeleteFile(string name);

        IDirectoryEntry OpenDirectory(string name, OpenMode mode);

        void DeleteDirectory(string name);

        void Flush();
    }
}