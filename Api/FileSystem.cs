using System;
using System.Collections.Generic;
using System.Threading;
using FS.Allocation;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Directory;

namespace FS.Api
{
    public sealed class FileSystem : IDisposable, IDirectoryCache
    {
        private readonly IBlockStorage storage;
        private readonly IAllocationManager allocationManager;
        private readonly Dictionary<int, DirectoryWithRefCount> openedDirectories = new Dictionary<int, DirectoryWithRefCount>();
        private readonly Dictionary<int, FileWithRefCount> openedFiles = new Dictionary<int, FileWithRefCount>();
        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool isDisposed;
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
            IIndex<int> AllocationIndexFactory(IAllocationManager m)
            {
                IIndexBlockProvider allocationIndexProvider = new IndexBlockProvider(header.AllocationBlock, m, storage);
                return new Index<int>(allocationIndexProvider, new BlockStream<int>(allocationIndexProvider), m, storage);
            }

            var allocationManager = new AllocationManager(AllocationIndexFactory, storage, header.FreeBlockCount);

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
            return new DirectoryEntry(this, rootDirectory, false);
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    WriteHeader();
                    rootDirectory.Flush();
                    allocationManager.Flush();

                    storage.Dispose();
                    cacheLock.Dispose();
                }

                isDisposed = true;
            }
        }

        private void WriteHeader()
        {
            var fsHeader = new FSHeader
            {
                AllocationBlock = allocationManager.BlockId,
                RootDirectoryBlock = rootDirectory.BlockId,
                FreeBlockCount = allocationManager.ReleasedBlockCount
            };
            storage.WriteBlock(0, new[] { fsHeader });
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        IBlockStorage IDirectoryCache.Storage => storage;

        IAllocationManager IDirectoryCache.AllocationManager => allocationManager;

        IDirectory IDirectoryCache.ReadDirectory(int blockId)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (openedDirectories.TryGetValue(blockId, out var directoryWithRefCount)) {

                    var directory = Volatile.Read(ref directoryWithRefCount.Directory);
                    if (directory != null)
                    {
                        Interlocked.Increment(ref directoryWithRefCount.RefCount);
                        return directoryWithRefCount.Directory;
                    }
                }

                cacheLock.EnterWriteLock();
                try
                {
                    if (openedDirectories.TryGetValue(blockId, out directoryWithRefCount))
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
                    openedDirectories.Add(blockId, directoryWithRefCount);
                    return directoryWithRefCount.Directory;
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        IDirectory IDirectoryCache.RegisterDirectory(IDirectory directory)
        {
            cacheLock.EnterWriteLock();
            try
            {
                var blockId = directory.BlockId;
                if (openedDirectories.TryGetValue(blockId, out var directoryWithRefCount))
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
                openedDirectories[blockId] = directoryWithRefCount;
                return directory;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        void IDirectoryCache.UnRegisterDirectory(int blockId)
        {
            cacheLock.EnterWriteLock();
            try
            {
                if (openedDirectories.TryGetValue(blockId, out var directoryWithRefCount))
                {
                    var existingDirectory = Volatile.Read(ref directoryWithRefCount.Directory);
                    if (existingDirectory == null)
                    {
                        throw new InvalidOperationException($"Directory with blockId = {blockId} has already been registered");
                    }
                    if (Interlocked.Decrement(ref directoryWithRefCount.RefCount) == 0)
                    {
                        openedDirectories.Remove(blockId);
                        directoryWithRefCount.Directory.Dispose();
                    }
                    return;
                }
                throw new InvalidOperationException($"Directory with blockId = {blockId} has already been registered");
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        IFile IDirectoryCache.ReadFile(int blockId, Func<IFile> readFile)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (openedFiles.TryGetValue(blockId, out var fileWithRefCount))
                {

                    var file = Volatile.Read(ref fileWithRefCount.File);
                    if (file != null)
                    {
                        Interlocked.Increment(ref fileWithRefCount.RefCount);
                        return fileWithRefCount.File;
                    }
                }

                cacheLock.EnterWriteLock();
                try
                {
                    if (openedFiles.TryGetValue(blockId, out fileWithRefCount))
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
                    openedFiles.Add(blockId, fileWithRefCount);
                    return fileWithRefCount.File;
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        IFile IDirectoryCache.RegisterFile(IFile file)
        {
            cacheLock.EnterWriteLock();
            try
            {
                var blockId = file.BlockId;
                if (openedFiles.TryGetValue(blockId, out var fileWithRefCount))
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
                openedFiles[blockId] = fileWithRefCount;
                return file;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        void IDirectoryCache.UnRegisterFile(int blockId)
        {
            cacheLock.EnterWriteLock();
            try
            {
                if (openedFiles.TryGetValue(blockId, out var fileWithRefCount))
                {
                    var existingDirectory = Volatile.Read(ref fileWithRefCount.File);
                    if (existingDirectory == null)
                    {
                        throw new InvalidOperationException($"File with blockId = {blockId} has already been registered");
                    }
                    if (Interlocked.Decrement(ref fileWithRefCount.RefCount) == 0)
                    {
                        openedFiles.Remove(blockId);
                        fileWithRefCount.File.Dispose();
                    }
                    return;
                }
                throw new InvalidOperationException($"File with blockId = {blockId} has already been registered");
            }
            finally
            {
                cacheLock.ExitWriteLock();
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
