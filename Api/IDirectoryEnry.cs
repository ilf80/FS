using System;

namespace FS.Api
{
    public interface IDirectoryEntry : IDisposable
    {
        IFileSystemEntry[] GetEntries();

        IFileSystemEntry FindOrDefault(string name);

        IFileEntry OpenFile(string name, OpenMode mode);

        void DeleteFile(string name);

        IDirectoryEntry OpenDirectory(string name, OpenMode mode);

        void DeleteDirectory(string name);

        void Flush();
    }
}
