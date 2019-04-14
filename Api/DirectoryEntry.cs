using System;
using System.Collections.Generic;
using System.Linq;
using FS.Directory;

namespace FS.Api
{
    public sealed class DirectoryEntry : IDirectoryEntry
    {
        private IDirectoryCache directoryCache;
        private IDirectory directory;
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

            directory.Flush();

            if (unRegisterDirectoryOnDispose)
            {
                directoryCache.UnRegisterDirectory(directory.BlockId);
            }
            directoryCache = null;
            directory = null;
        }

        public IFileSystemEntry FindOrDefault(string name)
        {
            return directory.GetDirectoryEntries()
                .Where(x => x.Name == name)
                .Select(x => new FileSystemEntry(x))
                .FirstOrDefault();
        }

        public void Flush()
        {
            directory.Flush();
        }

        public IEnumerable<IFileSystemEntry> GetEntries()
        {
            return directory.GetDirectoryEntries().Select(x => new FileSystemEntry(x)).ToArray();
        }

        public IDirectoryEntry OpenDirectory(string name, OpenMode mode)
        {
            var tempDirectory = directory.OpenDirectory(name, mode);
            return new DirectoryEntry(directoryCache, tempDirectory);
        }

        public IFileEntry OpenFile(string name, OpenMode mode)
        {
            var file = directory.OpenFile(name, mode);
            return new FileEntry(directoryCache, file);
        }

        public void DeleteFile(string name)
        {
            directory.DeleteFile(name);            
        }

        public void DeleteDirectory(string name)
        {
            directory.DeleteDirectory(name);
        }
    }
}
