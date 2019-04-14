using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FS.Api;
using FS.Core.Api.Directory;

namespace FS
{
    internal sealed class DirectoryEntry : IDirectoryEntry
    {
        private IDirectoryCache directoryCache;
        private IDirectory directory;
        private bool isDisposed;
        private readonly bool unRegisterDirectoryOnDispose;

        internal DirectoryEntry(
            IDirectoryCache directoryCache,
            IDirectory directory,
            bool unRegisterDirectoryOnDispose = true)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.unRegisterDirectoryOnDispose = unRegisterDirectoryOnDispose;
        }

        public void Dispose()
        {
            if (directory == null || directoryCache == null) return;
            isDisposed = true;

            directory?.Flush();

            if (unRegisterDirectoryOnDispose)
            {
                directoryCache?.UnRegisterDirectory(directory.BlockId);
            }
            directoryCache = null;
            directory = null;
        }

        public IFileSystemEntry FindOrDefault(string name)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            return directory.GetDirectoryEntries()
                .Where(x => x.Name == name)
                .Select(x => new FileSystemEntry(x))
                .FirstOrDefault();
        }

        public void Flush()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));

            directory.Flush();
        }

        public IEnumerable<IFileSystemEntry> GetEntries()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));

            return directory.GetDirectoryEntries().Select(x => new FileSystemEntry(x)).ToArray();
        }

        public IDirectoryEntry OpenDirectory(string name, OpenMode mode)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            if (!Enum.IsDefined(typeof(OpenMode), mode))
                throw new InvalidEnumArgumentException(nameof(mode), (int) mode, typeof(OpenMode));

            var tempDirectory = directory.OpenDirectory(name, mode);
            return new DirectoryEntry(directoryCache, tempDirectory);
        }

        public IFileEntry OpenFile(string name, OpenMode mode)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            if (!Enum.IsDefined(typeof(OpenMode), mode))
                throw new InvalidEnumArgumentException(nameof(mode), (int) mode, typeof(OpenMode));

            var file = directory.OpenFile(name, mode);
            return new FileEntry(directoryCache, file);
        }

        public void DeleteFile(string name)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            directory.DeleteFile(name);
        }

        public void DeleteDirectory(string name)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DirectoryEntry));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            directory.DeleteDirectory(name);
        }
    }
}
