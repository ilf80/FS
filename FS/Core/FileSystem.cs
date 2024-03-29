﻿using System;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class FileSystem : IFileSystem
    {
        private readonly IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool> directoryFactory;
        private bool isDisposed;
        private bool isOpened;
        private IFileSystemProvider provider;

        public FileSystem(
            IFactory<IFileSystemProvider> providerFactory,
            IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool> directoryFactory)
        {
            if (providerFactory == null)
            {
                throw new ArgumentNullException(nameof(providerFactory));
            }

            provider = providerFactory.Create();
            this.directoryFactory = directoryFactory ?? throw new ArgumentNullException(nameof(directoryFactory));
        }

        public void Open(string fileName, OpenMode openMode)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (isOpened)
            {
                throw new InvalidOperationException($"{nameof(FileSystem)} is already opened");
            }

            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(FileSystem));
            }

            isOpened = true;

            provider.Open(fileName, openMode);
        }

        public IDirectoryEntry GetRootDirectory()
        {
            if (!isOpened)
            {
                throw new InvalidOperationException($"{nameof(FileSystem)} is not initialized");
            }

            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(FileSystem));
            }

            return directoryFactory.Create(provider.DirectoryCache, provider.RootDirectory, false);
        }

        public void Dispose()
        {
            provider?.Dispose();
            provider = null;
            isDisposed = true;
        }
    }
}