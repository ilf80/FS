using System;
using System.Collections.Generic;

namespace FS.Api
{
    public interface IDirectoryEntry : IDisposable
    {
        IEnumerable<IFileSystemEntry> GetEntries();

        IFileSystemEntry FindOrDefault(string name);

        IFileEntry OpenFile(string name, OpenMode mode);

        void DeleteFile(string name);

        IDirectoryEntry OpenDirectory(string name, OpenMode mode);

        void DeleteDirectory(string name);

        void Flush();
    }
}
