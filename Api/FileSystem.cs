using FS.Allocattion;
using FS.Contracts;
using FS.Contracts.Indexes;
using FS.Directory;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FS.Api
{
    public sealed class FileSystem : IDisposable, IDirectoryCache
    {
        private readonly IBlockStorage storage;
        private readonly IAllocationManager allocationManager;
        private readonly Dictionary<int, DirectoryWithRefCount> openedDirectories = new Dictionary<int, DirectoryWithRefCount>();
        private readonly Dictionary<int, FileWithRefCount> openedFiles = new Dictionary<int, FileWithRefCount>();
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

            try
            {
                var header = new FSHeader[1];
                storage.ReadBlock(0, header);

                return ReadFromHeader(header[0], storage);
            }
            catch
            {
                storage.Dispose();
                throw;
            }
        }

        private static FileSystem ReadFromHeader(FSHeader header, IBlockStorage storage)
        {
            Func<IAllocationManager, IIndex<int>> allocationIndexFactory = (IAllocationManager m) =>
            {
                IIndexBlockProvier allocationIndexProvider = new IndexBlockProvier(header.AllocationBlock, m, storage);
                return new Index<int>(allocationIndexProvider, new BlockStream<int>(allocationIndexProvider), m, storage);
            };
            var allocationManager = new AllocationManager(allocationIndexFactory, storage, header.FreeBlockCount);

            var result = new FileSystem(storage, allocationManager);
            var rootDirectory = ((IDirectoryCache)result).ReadDirectory(header.RootDirectoryBlock);
            result.rootDirectory = rootDirectory;

            return result;
        }

        public static FileSystem Create(string fileName)
        {
            var storage = new BlockStorage(fileName);
            storage.Open();

            try
            {
                var buffer = new byte[512];
                var fsHeader = new FSHeader
                {
                    AllocationBlock = 1,
                    RootDirectoryBlock = 2,
                    FreeBlockCount = 0
                };
                storage.WriteBlock(0, new[] { fsHeader });
                storage.WriteBlock(1, buffer);

                buffer[0] = 3;
                storage.WriteBlock(2, buffer);

                var fsRoot = new DirectoryHeader
                {
                    FirstEmptyItemOffset = 1,
                    ItemsCount = 0,
                    LastNameOffset = 0,
                    NameBlockIndex = 4,
                    ParentDirectoryBlockIndex = 2
                };
                storage.WriteBlock(3, new[] { new DirectoryHeaderRoot { Header = fsRoot } });

                buffer[0] = 0;
                storage.WriteBlock(4, buffer);

                return ReadFromHeader(fsHeader, storage);
            }
            catch
            {
                storage.Dispose();
                throw;
            }
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
                    WriteHeader();
                    this.rootDirectory.Flush();
                    this.allocationManager.Flush();
                    this.storage.Dispose();
                    this.cacheLock.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private void WriteHeader()
        {
            var fsHeader = new FSHeader
            {
                AllocationBlock = this.allocationManager.BlockId,
                RootDirectoryBlock = this.rootDirectory.BlockId,
                FreeBlockCount = this.allocationManager.ReleasedBlockCount
            };
            this.storage.WriteBlock(0, new[] { fsHeader });
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        IBlockStorage IDirectoryCache.Storage => this.storage;

        IAllocationManager IDirectoryCache.AllocationManager => this.allocationManager;

        IDirectory IDirectoryCache.ReadDirectory(int blockId)
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
                        Directory = Directory.Directory.ReadDirectoryUnsafe(blockId, this),
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

        IDirectory IDirectoryCache.RegisterDirectory(IDirectory directory)
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

        void IDirectoryCache.UnRegisterDirectory(int blockId)
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

        IFile IDirectoryCache.ReadFile(int blockId, Func<IFile> readFile)
        {
            this.cacheLock.EnterUpgradeableReadLock();
            try
            {
                FileWithRefCount fileWithRefCount;
                if (this.openedFiles.TryGetValue(blockId, out fileWithRefCount))
                {

                    var file = Volatile.Read(ref fileWithRefCount.File);
                    if (file != null)
                    {
                        Interlocked.Increment(ref fileWithRefCount.RefCount);
                        return fileWithRefCount.File;
                    }
                }

                this.cacheLock.EnterWriteLock();
                try
                {
                    if (this.openedFiles.TryGetValue(blockId, out fileWithRefCount))
                    {
                        var file = Volatile.Read(ref fileWithRefCount.File);
                        if (file != null)
                        {
                            Interlocked.Increment(ref fileWithRefCount.RefCount);
                            return fileWithRefCount.File;
                        }
                    }
                    fileWithRefCount = new FileWithRefCount
                    {
                        File = readFile(),
                        RefCount = 1
                    };
                    this.openedFiles.Add(blockId, fileWithRefCount);
                    return fileWithRefCount.File;
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

        IFile IDirectoryCache.RegisterFile(IFile file)
        {
            this.cacheLock.EnterWriteLock();
            try
            {
                var blockId = file.BlockId;
                FileWithRefCount fileWithRefCount;
                if (this.openedFiles.TryGetValue(blockId, out fileWithRefCount))
                {
                    var existingFile = Volatile.Read(ref fileWithRefCount.File);
                    if (existingFile != null)
                    {
                        throw new InvalidOperationException($"File with blockId = {file.BlockId} has already been registered");
                    }
                }

                fileWithRefCount = new FileWithRefCount
                {
                    File = file,
                    RefCount = 1
                };
                this.openedFiles[blockId] = fileWithRefCount;
                return file;
            }
            finally
            {
                this.cacheLock.ExitWriteLock();
            }
        }

        void IDirectoryCache.UnRegisterFile(int blockId)
        {
            this.cacheLock.EnterWriteLock();
            try
            {
                FileWithRefCount fileWithRefCount;
                if (this.openedFiles.TryGetValue(blockId, out fileWithRefCount))
                {
                    var existingDirectory = Volatile.Read(ref fileWithRefCount.File);
                    if (existingDirectory == null)
                    {
                        throw new InvalidOperationException($"File with blockId = {blockId} has already been registered");
                    }
                    if (Interlocked.Decrement(ref fileWithRefCount.RefCount) == 0)
                    {
                        this.openedFiles.Remove(blockId);
                        fileWithRefCount.File.Dispose();
                    }
                    return;
                }
                throw new InvalidOperationException($"File with blockId = {blockId} has already been registered");
            }
            finally
            {
                this.cacheLock.ExitWriteLock();
            }
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

        private class FileWithRefCount
        {
            public int RefCount;

            public IFile File;
        }
    }
}
