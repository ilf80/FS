using System;

namespace FS
{
    internal interface IFileSystemProvider : IDisposable, ISupportsFlush
    {
        IDirectoryCache DirectoryCache { get; }

        IDirectory RootDirectory { get; }

        void Open(string fileName, OpenMode openMode);
    }
}