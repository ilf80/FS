using System;
using FS.Api;
using FS.Core.Api.Common;
using FS.Core.Api.Directory;

namespace FS.Core.Api.FileSystem
{
    public interface IFileSystemProvider : IDisposable, ISupportsFlush
    {
        IDirectoryCache DirectoryCache { get; }

        IDirectory RootDirectory { get; }

        void Open(string fileName, OpenMode openMode);
    }
}
