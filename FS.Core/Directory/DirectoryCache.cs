using System;
using System.Collections.Generic;
using System.Threading;
using FS.Api.Container;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.Common;
using FS.Core.Api.Directory;

namespace FS.Core.Directory
{
    internal sealed class DirectoryCache : IDirectoryCache
    {
        private readonly ICommonAccessParameters accessParameters;
        private readonly Dictionary<int, DirectoryWithRefCount> openedDirectories = new Dictionary<int, DirectoryWithRefCount>();
        private readonly Dictionary<int, FileWithRefCount> openedFiles = new Dictionary<int, FileWithRefCount>();
        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly IUnsafeDirectoryReader unsafeDirectoryReader;
        private bool isDisposed;

        public DirectoryCache(
            ICommonAccessParameters accessParameters,
            IFactory<IUnsafeDirectoryReader, IDirectoryCache> unsafeDirectoryReaderFactory)
        {
            if (unsafeDirectoryReaderFactory == null)
                throw new ArgumentNullException(nameof(unsafeDirectoryReaderFactory));
            this.accessParameters = accessParameters ?? throw new ArgumentNullException(nameof(accessParameters));
            unsafeDirectoryReader = unsafeDirectoryReaderFactory.Create(this);
        }

        public IBlockStorage Storage => accessParameters.Storage;

        public IAllocationManager AllocationManager => accessParameters.AllocationManager;

        public IDirectory ReadDirectory(int blockId)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (openedDirectories.TryGetValue(blockId, out var directoryWithRefCount))
                {

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
                        Directory = unsafeDirectoryReader.Read(blockId),
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

        public IDirectory RegisterDirectory(IDirectory directory)
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

        public void UnRegisterDirectory(int blockId)
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

        public IFile ReadFile(int blockId, Func<IFile> readFile)
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

        public IFile RegisterFile(IFile file)
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

        public void UnRegisterFile(int blockId)
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

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    cacheLock.Dispose();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        ~DirectoryCache()
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
