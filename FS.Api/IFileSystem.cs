using System;

namespace FS.Api
{
    public interface IFileSystem : IDisposable
    {
        void Open(string fileName, OpenMode openMode);

        IDirectoryEntry GetRootDirectory();
    }
}
