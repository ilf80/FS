using System;

namespace FS
{
    public interface IFileSystem : IDisposable
    {
        void Open(string fileName, OpenMode openMode);

        IDirectoryEntry GetRootDirectory();
    }
}