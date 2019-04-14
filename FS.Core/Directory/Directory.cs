using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FS.Api;
using FS.Api.Container;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Api.Common;
using FS.Core.Api.Directory;
using FS.Core.Utils;

namespace FS.Core.Directory
{
    internal sealed class Directory : IUnsafeDirectory
    {
        private readonly IIndex<DirectoryItem> index;
        private readonly IDirectoryCache directoryCache;
        private readonly IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory;
        private readonly IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters> directoryIndexFactory;
        private readonly IFactory<IDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader> directoryFactory;
        private readonly IFactory<IFile, IFileParameters, IDirectoryCache> fileFactory;
        private readonly IFactory<IDeletionFile, IFileParameters, IDirectoryCache> deletionFileFactory;
        private readonly IFactory<IDeletionDirectory, int, IDirectoryCache> deletionDirectoryFactory;
        private readonly int parentDirectoryBlockId;
        private readonly IBlockStream<DirectoryItem> blockStream;
        private readonly IIndex<short> nameIndex;
        private readonly IBlockStream<short> nameIndexBlockStream;
        private readonly ReaderWriterLockSlim indexLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly int nameBlockIndex;

        private int firstEmptyItemOffset;
        private int itemsCount;
        private int lastNameOffset;

        public Directory(
            IIndex<DirectoryItem> index,
            IDirectoryCache directoryCache,
            DirectoryHeader header,
            IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory,
            IFactory<IIndex<short>, IIndexBlockProvider, ICommonAccessParameters> indexFactory,
            IFactory<IBlockStream<short>, IBlockProvider<short>> blockStreamFactory,
            IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters> directoryIndexFactory,
            IFactory<IDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader> directoryFactory,
            IFactory<IFile, IFileParameters, IDirectoryCache> fileFactory,
            IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>> directoryBlockStreamFactory,
            IFactory<IDeletionFile, IFileParameters, IDirectoryCache> deletionFileFactory,
            IFactory<IDeletionDirectory, int, IDirectoryCache> deletionDirectoryFactory
                )
        {
            if (indexFactory == null) throw new ArgumentNullException(nameof(indexFactory));
            if (directoryBlockStreamFactory == null)
                throw new ArgumentNullException(nameof(directoryBlockStreamFactory));
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.indexBlockProviderFactory = indexBlockProviderFactory ?? throw new ArgumentNullException(nameof(indexBlockProviderFactory));
            this.directoryIndexFactory = directoryIndexFactory ?? throw new ArgumentNullException(nameof(directoryIndexFactory));
            this.directoryFactory = directoryFactory ?? throw new ArgumentNullException(nameof(directoryFactory));
            this.fileFactory = fileFactory ?? throw new ArgumentNullException(nameof(fileFactory));
            this.deletionFileFactory = deletionFileFactory ?? throw new ArgumentNullException(nameof(deletionFileFactory));
            this.deletionDirectoryFactory = deletionDirectoryFactory ?? throw new ArgumentNullException(nameof(deletionDirectoryFactory));

            blockStream = directoryBlockStreamFactory.Create(index);

            nameBlockIndex = header.NameBlockIndex;
            var nameIndexProvider = indexBlockProviderFactory.Create(nameBlockIndex, directoryCache);
            nameIndex = indexFactory.Create(nameIndexProvider, directoryCache);
            nameIndexBlockStream = blockStreamFactory.Create(nameIndex);

            firstEmptyItemOffset = header.FirstEmptyItemOffset;
            itemsCount = header.ItemsCount;
            lastNameOffset = header.LastNameOffset;
            parentDirectoryBlockId = header.ParentDirectoryBlockIndex;
        }

        public int BlockId => index.BlockId;

        public IDirectory OpenDirectory(string name, OpenMode openMode)
        {
            CheckName(name);

            indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => x.Name == name);
                if (entry != null)
                {
                    if (openMode == OpenMode.Create)
                    {
                        throw new InvalidOperationException($"Directory with '{name}' already exists");
                    }
                    return directoryCache.ReadDirectory(entry.BlockId);
                }
                if (openMode == OpenMode.OpenExisting)
                {
                    throw new InvalidOperationException($"Directory with '{name}' does not exist");
                }

                indexLock.EnterWriteLock();
                try
                {
                    var blocks = directoryCache.AllocationManager.Allocate(2);

                    var directoryIndexProvider = indexBlockProviderFactory.Create(blocks[1], directoryCache);
                    var directoryIndex = directoryIndexFactory.Create(directoryIndexProvider, directoryCache);
                    directoryIndex.SetSizeInBlocks(1);
                    directoryIndex.Flush();

                    var header = new DirectoryHeader
                    {
                        FirstEmptyItemOffset = 1,
                        ItemsCount = 0,
                        LastNameOffset = 0,
                        NameBlockIndex = blocks[0],
                        ParentDirectoryBlockIndex = index.BlockId
                    };

                    var directory = directoryFactory.Create(directoryIndex, directoryCache, header);
                    AddEntry(directoryIndex.BlockId, name, DirectoryFlags.Directory);
                    directory.Flush();

                    return directoryCache.RegisterDirectory(directory);
                }
                finally
                {
                    indexLock.ExitWriteLock();
                }
            }
            finally
            {
                indexLock.ExitUpgradeableReadLock();
            }
        }

        public IFile OpenFile(string name, OpenMode openMode)
        {
            CheckName(name);

            indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => x.Name == name);
                if (entry != null)
                {
                    if (openMode == OpenMode.Create)
                    {
                        throw new InvalidOperationException($"File with '{name}' already exists");
                    }
                    return directoryCache.ReadFile(
                        entry.BlockId,
                        () => fileFactory.Create(new FileParameters(entry.BlockId, BlockId, entry.Size), directoryCache));
                }
                if (openMode == OpenMode.OpenExisting)
                {
                    throw new InvalidOperationException($"Directory with '{name}' does not exist");
                }

                indexLock.EnterWriteLock();
                try
                {
                    var blocks = directoryCache.AllocationManager.Allocate(1);
                    AddEntry(blocks[0], name, DirectoryFlags.File);

                    var result = fileFactory.Create(new FileParameters(blocks[0], BlockId, 0), directoryCache);
                    result.SetSize(1);
                    result.Flush();

                    return directoryCache.RegisterFile(result);
                }
                finally
                {
                    indexLock.ExitWriteLock();
                }
            }
            finally
            {
                indexLock.ExitUpgradeableReadLock();
            }
        }

        public void DeleteFile(string name)
        {
            CheckName(name);

            indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => !x.IsDirectory && x.Name == name);
                if (entry == null)
                {
                    throw new InvalidOperationException($"File with name '{name}' does not exist");
                }

                indexLock.EnterWriteLock();
                try
                {
                    var deletionFile = deletionFileFactory.Create(
                        new FileParameters(entry.BlockId, BlockId, 0),
                        directoryCache);
                    var resultFile = directoryCache.ReadFile(
                        entry.BlockId,
                        () => deletionFile);

                    if (deletionFile != resultFile)
                    {
                        throw new InvalidOperationException($"File with name '{name}' is in use");
                    }
                    deletionFile.Delete();
                }
                finally
                {
                    indexLock.ExitWriteLock();
                }
            }
            finally
            {
                indexLock.ExitUpgradeableReadLock();
            }
        }

        public void DeleteDirectory(string name)
        {
            CheckName(name);

            indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => x.IsDirectory && x.Name == name);
                if (entry == null)
                {
                    throw new InvalidOperationException($"Directory with name '{name}' does not exist");
                }

                indexLock.EnterWriteLock();
                try
                {
                    var deletionDirectory = deletionDirectoryFactory.Create(entry.BlockId, directoryCache);
                    var resultDirectory = directoryCache.RegisterDirectory(deletionDirectory);

                    if (deletionDirectory != resultDirectory)
                    {
                        throw new InvalidOperationException($"Directory with name '{name}' is in use");
                    }

                    deletionDirectory.Delete();
                }
                finally
                {
                    indexLock.ExitWriteLock();
                }
            }
            finally
            {
                indexLock.ExitUpgradeableReadLock();
            }
        }

        public IDirectoryEntryInfo[] GetDirectoryEntries()
        {
            indexLock.EnterReadLock();
            try
            {
                List<IDirectoryEntryInfo> result = new List<IDirectoryEntryInfo>(itemsCount);
                if (itemsCount > 0)
                {
                    var buffer = new DirectoryItem[itemsCount];
                    blockStream.Read(1, buffer);

                    var names = new short[lastNameOffset];
                    nameIndexBlockStream.Read(0, names);

                    foreach (var item in buffer.Where(x => (x.Entry.Flags & DirectoryFlags.Deleted) == 0))
                    {
                        var entry = item.Entry;

                        var nameLength = names[entry.NameOffset];
                        var nameBuffer = new char[nameLength];
                        for (var i = 0; i < nameLength; i++)
                        {
                            nameBuffer[i] = (char)names[entry.NameOffset + 1 + i];
                        }

                        result.Add(new DirectoryEntryInfo(entry, new string(nameBuffer)));
                    }
                }
                return result.ToArray();
            }
            finally
            {
                indexLock.ExitReadLock();
            }
        }

        public void Flush()
        {
            indexLock.EnterReadLock();
            try
            {
                UpdateHeader();
                UpdateAccessTime();

                index.Flush();
                nameIndex.Flush();
            }
            finally
            {
                indexLock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            indexLock.Dispose();
        }

        public void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides overrides)
        {
            if (overrides == null) throw new ArgumentNullException(nameof(overrides));
            if (blockId < 0) throw new ArgumentOutOfRangeException(nameof(blockId));

            indexLock.EnterWriteLock();
            try
            {
                var buffer = new DirectoryItem[itemsCount];
                blockStream.Read(1, buffer);

                for (var i = 0; i < buffer.Length; i++)
                {
                    var entry = buffer[i];
                    if (entry.Entry.BlockIndex == blockId && (entry.Entry.Flags & DirectoryFlags.Deleted) == 0)
                    {
                        ApplyOverrides(ref entry.Entry, overrides);
                        blockStream.Write(i + 1, new[] { entry });
                        return;
                    }
                }
            }
            finally
            {
                indexLock.ExitWriteLock();
            }

            throw new Exception("Entry not found");
        }

        public void UnsafeDeleteDirectory()
        {
            index.SetSizeInBlocks(0);
            nameIndex.SetSizeInBlocks(0);
            directoryCache.AllocationManager.Release(new[] { BlockId, nameIndex.BlockId });

            UpdateParentDirectory(new DirectoryEntryInfoOverrides(flags: DirectoryFlags.Directory | DirectoryFlags.Deleted));
        }

        private void ApplyOverrides(ref DirectoryEntryStruct entry, IDirectoryEntryInfoOverrides overrides)
        {
            entry.Size = overrides.Size ?? entry.Size;
            entry.Updated = overrides.Updated?.Ticks ?? entry.Updated;
            entry.Flags = overrides.Flags ?? entry.Flags;
        }

        private void AddEntry(int blockId, string name, DirectoryFlags flags)
        {
            indexLock.EnterWriteLock();
            try
            {
                var directoryEntryItem = new DirectoryItem
                {
                    Entry = new DirectoryEntryStruct
                    {
                        BlockIndex = blockId,
                        Created = DateTime.Now.Ticks,
                        Updated = DateTime.Now.Ticks,
                        Size = 0,
                        Flags = flags,
                        NameOffset = StoreName(name)
                    }
                };
                AddEntry(directoryEntryItem);
            }
            finally
            {
                indexLock.ExitWriteLock();
            }
        }
        private void AddEntry(DirectoryItem directoryEntryItem)
        {
            blockStream.Write(firstEmptyItemOffset, new[] { directoryEntryItem });

            firstEmptyItemOffset = FindEmptyItem(firstEmptyItemOffset);
            itemsCount++;

            Flush();
        }

        private int FindEmptyItem(int startIndex)
        {
            startIndex++;

            var emptyEntryIndex = default(int?);
            var maxCapacity = index.SizeInBlocks * index.BlockSize;
            if (maxCapacity > startIndex)
            {
                var buffer = new DirectoryItem[maxCapacity - startIndex];
                blockStream.Read(startIndex, buffer);

                emptyEntryIndex = buffer
                    .Where(x => x.Entry.Flags == DirectoryFlags.None || (x.Entry.Flags & DirectoryFlags.Deleted) != 0)
                    .Select((x, entryIndex) => (int?)entryIndex)
                    .FirstOrDefault();
            }

            if (emptyEntryIndex == null)
            {
                index.SetSizeInBlocks(index.SizeInBlocks + 1);
                return startIndex;
            }
            return startIndex + emptyEntryIndex.Value;
        }

        private void UpdateHeader()
        {
            var directoryHeaderItem = new DirectoryItem
            {
                Header = new DirectoryHeader
                {
                    FirstEmptyItemOffset = firstEmptyItemOffset,
                    NameBlockIndex = nameBlockIndex,
                    ItemsCount = itemsCount,
                    LastNameOffset = lastNameOffset,
                    ParentDirectoryBlockIndex = parentDirectoryBlockId
                }
            };
            blockStream.Write(0, new[] { directoryHeaderItem });
        }

        private void UpdateAccessTime()
        {
            if (index.BlockId == parentDirectoryBlockId)
            {
                return;
            }

            UpdateParentDirectory(new DirectoryEntryInfoOverrides(updated: DateTime.Now));
        }

        private void UpdateParentDirectory(IDirectoryEntryInfoOverrides overrides)
        {
            var directory = directoryCache.ReadDirectory(parentDirectoryBlockId);
            try
            {
                directory.UpdateEntry(index.BlockId, overrides);
            }
            finally
            {
                directoryCache.UnRegisterDirectory(directory.BlockId);
            }
        }

        private int StoreName(string name)
        {
            var result = lastNameOffset;
            nameIndex.SetSizeInBlocks(Helpers.ModBaseWithCeiling(lastNameOffset + name.Length + 1, nameIndex.BlockSize));
            nameIndexBlockStream.Write(lastNameOffset, new[] { (short)name.Length }.Concat(name.Select(x => (short)x)).ToArray());
            nameIndex.Flush();

            lastNameOffset += name.Length + 1;
            return result;
        }

        private void CheckName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("File or directory name cannot be empty", nameof(name));
            }
            if (name.Length >= short.MaxValue)
            {
                throw new ArgumentException($"File or directory name is too long. Mex length is {short.MaxValue}", nameof(name));
            }
        }
    }
}
