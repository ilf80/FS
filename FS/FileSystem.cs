using System;
using FS.Api;
using FS.Core.Api.FileSystem;

namespace FS
{
    internal sealed class FileSystem : IFileSystem
    {
        private IFileSystemProvider provider;
        private bool isDisposed;
        private bool isOpened;

        public FileSystem(IFileSystemProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public void Open(string fileName, OpenMode openMode)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (isOpened) throw new InvalidOperationException($"{nameof(FileSystem)} is already opened");
            isOpened = true;

            provider.Open(fileName, openMode);
        }

        public IDirectoryEntry GetRootDirectory()
        {
            if (!isOpened) throw new InvalidOperationException($"{nameof(FileSystem)} is not initialized");
            if (isDisposed) throw new ObjectDisposedException(nameof(FileSystem));

            return new DirectoryEntry(provider.DirectoryCache, provider.RootDirectory, false);
        }

        public void Dispose()
        {
            provider?.Dispose();
            provider = null;
            isDisposed = true;
        }
    }
}
