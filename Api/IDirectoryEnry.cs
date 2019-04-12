﻿using FS.Directory;
using System;
using System.Linq;

namespace FS.Api
{
    public interface IDirectoryEntry : IDisposable
    {
        IFileSystemEntry[] GetEntries();

        IFileSystemEntry Find(string name);

        IFileEntry OpenFile(string name, OpenMode mode);

        IDirectoryEntry OpenDirectory(string name, OpenMode mode);

        void Flush();
    }

    public sealed class DirectoryEntry : IDirectoryEntry
    {
        private IDirectoryManager directoryManager;
        private IDirectory directory;
        private readonly bool unregisterDirectoryOnDispose;

        internal DirectoryEntry(
            IDirectoryManager directoryManager,
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
                if (this.unregisterDirectoryOnDispose)
                {
                    this.directoryManager.UnRegisterDirectory(this.directory.BlockId);
                }
                this.directoryManager = null;
                this.directory = null;
            }
        }

        public IFileSystemEntry Find(string name)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}