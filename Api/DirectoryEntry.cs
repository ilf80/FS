using FS.Directory;
using System;
using System.Linq;

namespace FS.Api
{
    public sealed class DirectoryEntry : IDirectoryEntry
    {
        private IDirectoryCache directoryManager;
        private IDirectory directory;
        private readonly bool unregisterDirectoryOnDispose;

        internal DirectoryEntry(
            IDirectoryCache directoryManager,
            IDirectory directory,
            bool unregisterDirectoryOnDispose = true)
        {
            this.directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.unregisterDirectoryOnDispose = unregisterDirectoryOnDispose;
        }

        public void Dispose()
        {
            if (this.directory != null && this.directoryManager != null)
            {
                this.directory.Flush();

                if (this.unregisterDirectoryOnDispose)
                {
                    this.directoryManager.UnRegisterDirectory(this.directory.BlockId);
                }
                this.directoryManager = null;
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
            return new DirectoryEntry(this.directoryManager, directory);
        }

        public IFileEntry OpenFile(string name, OpenMode mode)
        {
            var file = this.directory.OpenFile(name, mode);
            return new FileEntry(this.directoryManager, file);
        }
    }
}
