using FS.Allocattion;
using FS.Contracts;
using FS.Contracts.Indexes;
using FS.Directory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading;

namespace FS.Api
{
    public sealed class FileSystem : IDisposable, IDirectoryManager
    {
        private readonly IBlockStorage storage;
        private readonly IAllocationManager allocationManager;
        private readonly Dictionary<int, DirectoryWithRefCount> openedDirectories = new Dictionary<int, DirectoryWithRefCount>();
        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool isDisposed = false;
        private IDirectory rootDirectory;

        private FileSystem(IBlockStorage storage, IAllocationManager allocationManager)
        {
            this.storage = storage;
            this.allocationManager = allocationManager;
        }

        public static FileSystem Open(string fileName)
        {
            var storage = new BlockStorage(fileName);

            storage.Open();

            var header = new FSHeader[1];
            storage.ReadBlock(0, header);

            Func<IAllocationManager, IIndex<int>> allocationIndexFactory = (IAllocationManager m) => {
                IIndexBlockProvier allocationIndexProvider = new IndexBlockProvier(header[0].AllocationBlock, m, storage);
                return new Index<int>(allocationIndexProvider, new BlockStream<int>(allocationIndexProvider), m, storage);
            };
            var allocationManager = new AllocationManager(allocationIndexFactory, storage, header[0].FreeBlockCount);

            var result = new FileSystem(storage, allocationManager);
            var rootDirectory = ((IDirectoryManager)result).ReadDirectory(header[0].RootDirectoryBlock);
            result.rootDirectory = rootDirectory;

            return result;
        }

        public static FileSystem Create(string fileName)
        {
            throw new NotImplementedException();
        }

        public IDirectoryEntry GetRootDirectory()
        {
            return new DirectoryEntry(this, this.rootDirectory, false);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.allocationManager.Flush();
                    this.storage.Dispose();
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        IBlockStorage IDirectoryManager.Storage => this.storage;

        IAllocationManager IDirectoryManager.AllocationManager => this.allocationManager;

        IDirectory IDirectoryManager.ReadDirectory(int blockId)
        {
            this.cacheLock.EnterUpgradeableReadLock();
            try
            {
                DirectoryWithRefCount directoryWithRefCount;
                if (this.openedDirectories.TryGetValue(blockId, out directoryWithRefCount)) {

                    var directory = Volatile.Read(ref directoryWithRefCount.Directory);
                    if (directory != null)
                    {
                        Interlocked.Increment(ref directoryWithRefCount.RefCount);
                        return directoryWithRefCount.Directory;
                    }
                }

                this.cacheLock.EnterWriteLock();
                try
                {
                    if (this.openedDirectories.TryGetValue(blockId, out directoryWithRefCount))
                    {
                        var directory = Volatile.Read(ref directoryWithRefCount.Directory);
                        if (directory != null)
                        {
                            Interlocked.Increment(ref directoryWithRefCount.RefCount);
                            return directoryWithRefCount.Directory;
                        }
                    }
                    directoryWithRefCount = new DirectoryWithRefCount
                    {
                        Directory = DirectoryManager.ReadDirectoryUnsafe(blockId, this),
                        RefCount = 1
                    };
                    this.openedDirectories.Add(blockId, directoryWithRefCount);
                    return directoryWithRefCount.Directory;
                }
                finally
                {
                    this.cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                this.cacheLock.ExitUpgradeableReadLock();
            }
        }

        IDirectory IDirectoryManager.RegisterDirectory(IDirectory directory)
        {
            this.cacheLock.EnterWriteLock();
            try
            {
                var blockId = directory.BlockId;
                DirectoryWithRefCount directoryWithRefCount;
                if (this.openedDirectories.TryGetValue(blockId, out directoryWithRefCount))
                {
                    var existingDirectory = Volatile.Read(ref directoryWithRefCount.Directory);
                    if (existingDirectory != null)
                    {
                        throw new InvalidOperationException($"Directory with blockId = {directory.BlockId} has already been registered");
                    }
                }

                directoryWithRefCount = new DirectoryWithRefCount
                {
                    Directory = directory,
                    RefCount = 1
                };
                this.openedDirectories[blockId] = directoryWithRefCount;
                return directory;
            }
            finally
            {
                this.cacheLock.ExitWriteLock();
            }
        }

        void IDirectoryManager.UnRegisterDirectory(int blockId)
        {
            this.cacheLock.EnterWriteLock();
            try
            {
                DirectoryWithRefCount directoryWithRefCount;
                if (this.openedDirectories.TryGetValue(blockId, out directoryWithRefCount))
                {
                    var existingDirectory = Volatile.Read(ref directoryWithRefCount.Directory);
                    if (existingDirectory == null)
                    {
                        throw new InvalidOperationException($"Directory with blockId = {blockId} has already been registered");
                    }
                    if (Interlocked.Decrement(ref directoryWithRefCount.RefCount) == 0)
                    {
                        this.openedDirectories.Remove(blockId);
                        directoryWithRefCount.Directory.Dispose();
                    }
                    return;
                }
                throw new InvalidOperationException($"Directory with blockId = {blockId} has already been registered");
            }
            finally
            {
                this.cacheLock.ExitWriteLock();
            }
        }

        IFile IDirectoryManager.RegisterFile(IFile file)
        {
            throw new NotImplementedException();
        }

        IFile IDirectoryManager.ReadFile(int blockId, Func<IFile> readFile)
        {
            throw new NotImplementedException();
        }

        ~FileSystem()
        {
            Dispose(false);
        }

        private class DirectoryWithRefCount
        {
            public int RefCount;

            public IDirectory Directory;
        }
    }
}
