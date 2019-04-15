using System;
using System.Collections.Generic;
using System.Linq;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class DirectoryEntry : IDirectoryEntry
    {
        private readonly IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool> directoryFactory;
        private readonly IFactory<IFileEntry, IDirectoryCache, IFile> fileFactory;
        private readonly bool unRegisterDirectoryOnDispose;
        private IDirectory directory;
        private IDirectoryCache directoryCache;
        private bool isDisposed;

        public DirectoryEntry(
            IDirectoryCache directoryCache,
            IDirectory directory,
            bool unRegisterDirectoryOnDispose,
            IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool> directoryFactory,
            IFactory<IFileEntry, IDirectoryCache, IFile> fileFactory)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.unRegisterDirectoryOnDispose = unRegisterDirectoryOnDispose;
            this.directoryFactory = directoryFactory ?? throw new ArgumentNullException(nameof(directoryFactory));
            this.fileFactory = fileFactory ?? throw new ArgumentNullException(nameof(fileFactory));
        }

        public void Dispose()
        {
            if (directory == null || directoryCache == null)
            {
                return;
            }

            isDisposed = true;

            directory?.Flush();

            if (unRegisterDirectoryOnDispose)
            {
                directoryCache?.UnRegisterDirectory(directory.BlockId);
            }

            directoryCache = null;
            directory = null;
        }

        public bool TryGetEntry(string name, out IFileSystemEntry entry)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            entry = directory.GetDirectoryEntries()
                .Where(x => x.Name == name)
                .Select(x => new FileSystemEntry(x))
                .FirstOrDefault();
            return entry != null;
        }

        public void Flush()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            directory.Flush();
        }

        public IEnumerable<IFileSystemEntry> GetEntries()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            return directory.GetDirectoryEntries().Select(x => new FileSystemEntry(x)).ToArray();
        }

        public IDirectoryEntry OpenDirectory(string name, OpenMode mode)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (!Enum.IsDefined(typeof(OpenMode), mode))
            {
                throw new ArgumentException(nameof(mode));
            }

            var tempDirectory = directory.OpenDirectory(name, mode);
            return directoryFactory.Create(directoryCache, tempDirectory, true);
        }

        public IFileEntry OpenFile(string name, OpenMode mode)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (!Enum.IsDefined(typeof(OpenMode), mode))
            {
                throw new ArgumentException(nameof(mode));
            }

            var file = directory.OpenFile(name, mode);
            return fileFactory.Create(directoryCache, file);
        }

        public void DeleteFile(string name)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            directory.DeleteFile(name);
        }

        public void DeleteDirectory(string name)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DirectoryEntry));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            directory.DeleteDirectory(name);
        }
    }
}