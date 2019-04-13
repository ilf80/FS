using FS.Directory;
using System;
using System.Linq;

namespace FS.Api
{
    public sealed class DirectoryEntry : IDirectoryEntry
    {
        private IDirectoryCache directoryCache;
        private IDirectory directory;
        private readonly bool unregisterDirectoryOnDispose;

        internal DirectoryEntry(
            IDirectoryCache directoryCache,
            IDirectory directory,
            bool unregisterDirectoryOnDispose = true)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.unregisterDirectoryOnDispose = unregisterDirectoryOnDispose;
        }

        public void Dispose()
        {
            if (this.directory != null && this.directoryCache != null)
            {
                this.directory.Flush();

                if (this.unregisterDirectoryOnDispose)
                {
                    this.directoryCache.UnRegisterDirectory(this.directory.BlockId);
                }
                this.directoryCache = null;
                this.directory = null;
            }
        }

        public IFileSystemEntry FindOrDefault(string name)
        {
            return this.directory.GetDirectoryEntries()
                .Where(x => x.Name == name)
                .Select(x => new FileSystemEntry(x))
                .FirstOrDefault();
        }

        public void Flush()
        {
            this.directory.Flush();
        }

        public IFileSystemEntry[] GetEntries()
        {
            return this.directory.GetDirectoryEntries().Select(x => new FileSystemEntry(x)).ToArray();
        }

        public IDirectoryEntry OpenDirectory(string name, OpenMode mode)
        {
            var directory = this.directory.OpenDirectory(name, mode);
            return new DirectoryEntry(this.directoryCache, directory);
        }

        public IFileEntry OpenFile(string name, OpenMode mode)
        {
            var file = this.directory.OpenFile(name, mode);
            return new FileEntry(this.directoryCache, file);
        }

        public void DeleteFile(string name)
        {
            this.directory.DeleteFile(name);            
        }

        public void DeleteDirectory(string name)
        {
            this.directory.DeleteDirectory(name);
        }
    }
}
