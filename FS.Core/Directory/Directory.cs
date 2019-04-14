using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FS.Api;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Api.Directory;
using FS.Core.BlockAccess;
using FS.Core.BlockAccess.Indexes;
using FS.Core.Utils;

namespace FS.Core.Directory
{
    internal sealed class Directory : IDirectory
    {
        private readonly IIndex<DirectoryItem> index;
        private readonly IDirectoryCache directoryCache;
        private readonly int parentDirectoryBlockId;
        private readonly BlockStream<DirectoryItem> blockStream;
        private readonly Index<short> nameIndex;
        private readonly BlockStream<short> nameIndexBlockStream;
        private readonly ReaderWriterLockSlim indexLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly int nameBlockIndex;

        private int firstEmptyItemOffset;
        private int itemsCount;
        private int lastNameOffset;

        private Directory(
            IIndex<DirectoryItem> index,
            IDirectoryCache directoryCache,
            DirectoryHeader header)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            blockStream = new BlockStream<DirectoryItem>(index);
            nameBlockIndex = header.NameBlockIndex;

            var nameIndexProvider = new IndexBlockProvider(nameBlockIndex, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            var nameIndexProviderBlockStream = new BlockStream<int>(nameIndexProvider);
            nameIndex = new Index<short>(nameIndexProvider, nameIndexProviderBlockStream, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            nameIndexBlockStream = new BlockStream<short>(nameIndex);

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

                    var directoryIndexProvider = new IndexBlockProvider(blocks[1], directoryCache.AllocationManager, directoryCache.Storage);
                    var directoryIndexBlockStream = new BlockStream<int>(directoryIndexProvider);
                    var directoryIndex = new Index<DirectoryItem>(directoryIndexProvider, directoryIndexBlockStream, directoryCache.AllocationManager, directoryCache.Storage);
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
                    var directory = new Directory(directoryIndex, directoryCache, header);
                    directory.UpdateHeader();

                    AddEntry(directoryIndex.BlockId, name, DirectoryFlags.Directory);

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
                        () => new File(directoryCache, entry.BlockId, BlockId, entry.Size));
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

                    var result = new File(directoryCache, blocks[0], BlockId, 0);
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
                    var deletionFile = new DeletionFile(directoryCache, entry.BlockId, BlockId);
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
                    var deletionDirectory = new DeletionDirectory(directoryCache, entry.BlockId);
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

        internal void UnsafeDeleteDirectory()
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

            UpdateHeader();
            Flush();

            UpdateAccessTime();
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

        internal static Directory ReadDirectoryUnsafe(int blockId, IDirectoryCache directoryManager)
        {
            var indexBlockProvider = new IndexBlockProvider(blockId, directoryManager.AllocationManager, directoryManager.Storage);
            var index = new Index<DirectoryItem>(indexBlockProvider, new BlockStream<int>(indexBlockProvider), directoryManager.AllocationManager, directoryManager.Storage);
            var indexStream = new BlockStream<DirectoryItem>(index);

            var buffer = new DirectoryItem[1];
            indexStream.Read(0, buffer);
            var header = buffer[0].Header;

            return new Directory(index, directoryManager, header);
        }
    }
}
