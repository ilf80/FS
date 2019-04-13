using FS.Api;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FS.Directory
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

        private int nameBlockIndex;
        private int firstEmptyItemOffset;
        private int itemsCount;
        private int lastNameOffset;

        public Directory(
            IIndex<DirectoryItem> index,
            IDirectoryCache directoryCache,
            DirectoryHeader header)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.blockStream = new BlockStream<DirectoryItem>(index);
            this.nameBlockIndex = header.NameBlockIndex;

            var nameIndexProvider = new IndexBlockProvier(this.nameBlockIndex, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            var nameIndexProbiderBlockStream = new BlockStream<int>(nameIndexProvider);
            this.nameIndex = new Index<short>(nameIndexProvider, nameIndexProbiderBlockStream, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            this.nameIndexBlockStream = new BlockStream<short>(this.nameIndex);

            this.firstEmptyItemOffset = header.FirstEmptyItemOffset;
            this.itemsCount = header.ItemsCount;
            this.lastNameOffset = header.LastNameOffset;
            this.parentDirectoryBlockId = header.ParentDirectoryBlockIndex;
        }

        public int BlockId
        {
            get { return this.index.BlockId; }
        }

        public IDirectory OpenDirectory(string name, OpenMode openMode)
        {
            this.indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => x.Name == name);
                if (entry != null)
                {
                    if (openMode == OpenMode.Create)
                    {
                        throw new InvalidOperationException($"Directory with '{name}' already exists");
                    }
                    return this.directoryCache.ReadDirectory(entry.BlockId);
                }
                if (openMode == OpenMode.OpenExisting)
                {
                    throw new InvalidOperationException($"Directory with '{name}' does not exist");
                }

                this.indexLock.EnterWriteLock();
                try
                {
                    var blocks = this.directoryCache.AllocationManager.Allocate(2);

                    var directoryIndexProvider = new IndexBlockProvier(blocks[1], this.directoryCache.AllocationManager, this.directoryCache.Storage);
                    var directoryIndexBlockChain = new BlockStream<int>(directoryIndexProvider);
                    var directoryIndex = new Index<DirectoryItem>(directoryIndexProvider, directoryIndexBlockChain, this.directoryCache.AllocationManager, this.directoryCache.Storage);
                    directoryIndex.SetSizeInBlocks(1);
                    directoryIndex.Flush();

                    var header = new DirectoryHeader
                    {
                        FirstEmptyItemOffset = 1,
                        ItemsCount = 0,
                        LastNameOffset = 0,
                        NameBlockIndex = blocks[0],
                        ParentDirectoryBlockIndex = this.index.BlockId
                    };
                    var directory = new Directory(directoryIndex, this.directoryCache, header);
                    directory.UpdateHeader();

                    AddEntry(directoryIndex.BlockId, name, DirectoryFlags.Directory);

                    return this.directoryCache.RegisterDirectory(directory);
                }
                finally
                {
                    this.indexLock.ExitWriteLock();
                }
            }
            finally
            {
                this.indexLock.ExitUpgradeableReadLock();
            }
        }

        public IFile OpenFile(string name, OpenMode openMode)
        {
            this.indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => x.Name == name);
                if (entry != null)
                {
                    if (openMode == OpenMode.Create)
                    {
                        throw new InvalidOperationException($"File with '{name}' already exists");
                    }
                    return this.directoryCache.ReadFile(
                        entry.BlockId,
                        () => new File(this.directoryCache, entry.BlockId, BlockId, entry.Size));
                }
                if (openMode == OpenMode.OpenExisting)
                {
                    throw new InvalidOperationException($"Directory with '{name}' does not exist");
                }

                this.indexLock.EnterWriteLock();
                try
                {
                    var blocks = this.directoryCache.AllocationManager.Allocate(1);
                    AddEntry(blocks[0], name, DirectoryFlags.File);

                    var result = new File(this.directoryCache, blocks[0], BlockId, 0);
                    result.SetSize(1);
                    result.Flush();

                    return this.directoryCache.RegisterFile(result);
                }
                finally
                {
                    this.indexLock.ExitWriteLock();
                }
            }
            finally
            {
                this.indexLock.ExitUpgradeableReadLock();
            }
        }

        public void DeleteFile(string name)
        {
            this.indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().Where(x => !x.IsDirectory && x.Name == name).FirstOrDefault();
                if (entry == null)
                {
                    throw new InvalidOperationException($"File with name '{name}' does not exist");
                }

                this.indexLock.EnterWriteLock();
                try
                {
                    var deletionFile = new DeletionFile(this.directoryCache, entry.BlockId, BlockId, entry.Size);
                    var resultFile = this.directoryCache.ReadFile(
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
                    this.indexLock.ExitWriteLock();
                }
            }
            finally
            {
                this.indexLock.ExitUpgradeableReadLock();
            }
        }

        public void DeleteDirectory(string name)
        {
            this.indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().Where(x => x.IsDirectory && x.Name == name).FirstOrDefault();
                if (entry == null)
                {
                    throw new InvalidOperationException($"Directory with name '{name}' does not exist");
                }

                this.indexLock.EnterWriteLock();
                try
                {
                    var deletionDirectory = new DeletionDirectory(this.directoryCache, entry.BlockId);
                    var resultirectory = this.directoryCache.RegisterDirectory(deletionDirectory);

                    if (deletionDirectory != resultirectory)
                    {
                        throw new InvalidOperationException($"Directory with name '{name}' is in use");
                    }

                    deletionDirectory.Delete();
                }
                finally
                {
                    this.indexLock.ExitWriteLock();
                }
            }
            finally
            {
                this.indexLock.ExitUpgradeableReadLock();
            }
        }

        public IDirectoryEntryInfo[] GetDirectoryEntries()
        {
            this.indexLock.EnterReadLock();
            try
            {
                List<IDirectoryEntryInfo> result = new List<IDirectoryEntryInfo>(this.itemsCount);
                if (this.itemsCount > 0)
                {
                    var buffer = new DirectoryItem[this.itemsCount];
                    this.blockStream.Read(1, buffer);

                    var names = new short[this.lastNameOffset];
                    this.nameIndexBlockStream.Read(0, names);

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
                this.indexLock.ExitReadLock();
            }
        }

        public void Flush()
        {
            this.indexLock.EnterReadLock();
            try
            {
                this.index.Flush();
                this.nameIndex.Flush();
            }
            finally
            {
                this.indexLock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            this.indexLock.Dispose();
        }

        public void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides overrides)
        {
            this.indexLock.EnterWriteLock();
            try
            {
                var buffer = new DirectoryItem[this.itemsCount];
                this.blockStream.Read(1, buffer);

                for (var i = 0; i < buffer.Length; i++)
                {
                    var entry = buffer[i];
                    if (entry.Entry.BlockIndex == blockId && (entry.Entry.Flags & DirectoryFlags.Deleted) == 0)
                    {
                        ApplyOverrides(ref entry.Entry, overrides);
                        this.blockStream.Write(i + 1, new[] { entry });
                        return;
                    }
                }
            }
            finally
            {
                this.indexLock.ExitWriteLock();
            }

            throw new Exception("Entry not found");
        }

        internal void UnsafeDeleteDirectory()
        {
            this.index.SetSizeInBlocks(0);
            this.nameIndex.SetSizeInBlocks(0);
            this.directoryCache.AllocationManager.Release(new[] { BlockId, this.nameIndex.BlockId });

            UpdateParentDirectory(new DirectoryEntryInfoOverrides(flags: DirectoryFlags.Directory | DirectoryFlags.Deleted));
        }

        private void ApplyOverrides(ref DirectoryEntryStruct entry, IDirectoryEntryInfoOverrides overrides)
        {
            entry.Size = overrides.Size ?? entry.Size;
            entry.Updated = overrides.Updated.HasValue ? overrides.Updated.Value.Ticks : entry.Updated;
            entry.Flags = overrides.Flags ?? entry.Flags;
        }

        private void AddEntry(int blockId, string name, DirectoryFlags flags)
        {
            this.indexLock.EnterWriteLock();
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
                this.indexLock.ExitWriteLock();
            }
        }
        private void AddEntry(DirectoryItem directoryEntryItem)
        {
            this.blockStream.Write(this.firstEmptyItemOffset, new[] { directoryEntryItem });

            this.firstEmptyItemOffset = FindEmptyItem(this.firstEmptyItemOffset);
            this.itemsCount++;

            UpdateHeader();
            Flush();

            UpdateAccessTime();
        }

        private int FindEmptyItem(int firstEmptyItemOffset)
        {
            firstEmptyItemOffset++;

            var emptyEntryIndex = default(int?);
            var maxCapacity = this.index.SizeInBlocks * this.index.BlockSize;
            if (maxCapacity > firstEmptyItemOffset)
            {
                var buffer = new DirectoryItem[maxCapacity - firstEmptyItemOffset];
                this.blockStream.Read(firstEmptyItemOffset, buffer);

                emptyEntryIndex = buffer
                    .Where(x => x.Entry.Flags == DirectoryFlags.None || (x.Entry.Flags & DirectoryFlags.Deleted) != 0)
                    .Select((x, index) => (int?)index)
                    .FirstOrDefault();
            }

            if (emptyEntryIndex == null)
            {
                this.index.SetSizeInBlocks(this.index.SizeInBlocks + 1);
                return firstEmptyItemOffset;
            }
            return firstEmptyItemOffset + emptyEntryIndex.Value;
        }

        private void UpdateHeader()
        {
            var directoryHeaderItem = new DirectoryItem
            {
                Header = new DirectoryHeader
                {
                    FirstEmptyItemOffset = this.firstEmptyItemOffset,
                    NameBlockIndex = this.nameBlockIndex,
                    ItemsCount = this.itemsCount,
                    LastNameOffset = this.lastNameOffset,
                    ParentDirectoryBlockIndex = this.parentDirectoryBlockId
                }
            };
            this.blockStream.Write(0, new[] { directoryHeaderItem });
        }

        private void UpdateAccessTime()
        {
            if (this.index.BlockId == this.parentDirectoryBlockId)
            {
                return;
            }

            UpdateParentDirectory(new DirectoryEntryInfoOverrides(updated: DateTime.Now));
        }

        private void UpdateParentDirectory(IDirectoryEntryInfoOverrides overrides)
        {
            var directory = this.directoryCache.ReadDirectory(this.parentDirectoryBlockId);
            try
            {
                directory.UpdateEntry(this.index.BlockId, overrides);
            }
            finally
            {
                this.directoryCache.UnRegisterDirectory(directory.BlockId);
            }
        }

        private int StoreName(string name)
        {
            var result = this.lastNameOffset;
            this.nameIndex.SetSizeInBlocks(Helpers.ModBaseWithCeiling(this.lastNameOffset + name.Length + 1, this.nameIndex.BlockSize));
            this.nameIndexBlockStream.Write(this.lastNameOffset, new[] { (short)name.Length }.Concat(name.Select(x => (short)x)).ToArray());
            this.nameIndex.Flush();

            this.lastNameOffset += name.Length + 1;
            return result;
        }

        internal static Directory ReadDirectoryUnsafe(int blockId, IDirectoryCache directoryManager)
        {
            var indexBlockProvider = new IndexBlockProvier(blockId, directoryManager.AllocationManager, directoryManager.Storage);
            var index = new Index<DirectoryItem>(indexBlockProvider, new BlockStream<int>(indexBlockProvider), directoryManager.AllocationManager, directoryManager.Storage);
            var indexStream = new BlockStream<DirectoryItem>(index);

            var buffer = new DirectoryItem[1];
            indexStream.Read(0, buffer);
            var header = buffer[0].Header;

            return new Directory(index, directoryManager, header);
        }
    }
}
