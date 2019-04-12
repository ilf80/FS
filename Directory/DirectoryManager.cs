using FS.Allocattion;
using FS.Api;
using FS.Contracts;
using FS.Contracts.Indexes;
using FS.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FS.Directory
{
    internal class DirectoryManager: IDirectory
    {
        private readonly IIndex<DirectoryItem> index;
        private readonly IDirectoryManager directoryManager;
        private readonly int parentDirectoryBlockId;
        private readonly BlockStream<DirectoryItem> blockStream;
        private readonly Index<short> nameIndex;
        private readonly BlockStream<short> nameIndexBlockChain;
        private readonly ReaderWriterLockSlim indexLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private int nameBlockIndex;
        private int firstEmptyItemOffset;
        private int itemsCount;
        private int lastNameOffset;

        public DirectoryManager(
            IIndex<DirectoryItem> index,
            IDirectoryManager directoryManager,
            DirectoryHeader header)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            this.blockStream = new BlockStream<DirectoryItem>(index);
            this.nameBlockIndex = header.NameBlockIndex;

            var nameIndexProvider = new IndexBlockProvier(this.nameBlockIndex, this.directoryManager.AllocationManager, this.directoryManager.Storage);
            var nameIndexProbiderBlockStream = new BlockStream<int>(nameIndexProvider);
            this.nameIndex = new Index<short>(nameIndexProvider, nameIndexProbiderBlockStream, this.directoryManager.Storage, this.directoryManager.AllocationManager);
            this.nameIndexBlockChain = new BlockStream<short>(this.nameIndex);

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
                    return this.directoryManager.ReadDirectory(entry.BlockId);
                }

                this.indexLock.EnterWriteLock();
                try
                {
                    var blocks = this.directoryManager.AllocationManager.Allocate(2);

                    var directoryIndexProvider = new IndexBlockProvier(blocks[1], this.directoryManager.AllocationManager, this.directoryManager.Storage);
                    var directoryIndexBlockChain = new BlockStream<int>(directoryIndexProvider);
                    var directoryIndex = new Index<DirectoryItem>(directoryIndexProvider, directoryIndexBlockChain, this.directoryManager.Storage, this.directoryManager.AllocationManager);
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
                    var directory = new DirectoryManager(directoryIndex, this.directoryManager, header);
                    directory.UpdateHeader();

                    AddEntry(directoryIndex.BlockId, name, DirectoryFlags.Directory);

                    return this.directoryManager.RegisterDirectory(directory);
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

        public IFile OpenFile(string name)
        {
            this.indexLock.EnterUpgradeableReadLock();
            try
            {
                var entry = GetDirectoryEntries().FirstOrDefault(x => x.Name == name);
                if (entry != null)
                {
                    return this.directoryManager.ReadFile(
                        entry.BlockId,
                        () => new File(this.directoryManager, entry.BlockId, BlockId, entry.Size));
                }

                this.indexLock.EnterWriteLock();
                try
                {
                    var blocks = this.directoryManager.AllocationManager.Allocate(1);
                    AddEntry(blocks[0], name, DirectoryFlags.File);

                    var result = new File(this.directoryManager, blocks[0], BlockId, 0);
                    result.SetSize(1);
                    result.Flush();

                    return this.directoryManager.RegisterFile(result);
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
            List<IDirectoryEntryInfo> result;

            this.indexLock.EnterReadLock();
            try
            {
                var buffer = new DirectoryItem[this.itemsCount];
                this.blockStream.Read(1, buffer);

                var names = new short[this.lastNameOffset];
                this.nameIndexBlockChain.Read(0, names);

                result = new List<IDirectoryEntryInfo>(this.itemsCount);
                foreach (var item in buffer)
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
            finally
            {
                this.indexLock.ExitReadLock();
            }

            return result.ToArray();
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
                    if (entry.Entry.BlockIndex == blockId)
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

        private void ApplyOverrides(ref DirectoryEntryStruct entry, IDirectoryEntryInfoOverrides overrides)
        {
            entry.Size = overrides.Size ?? entry.Size;
            entry.Updated = overrides.Updated.HasValue ? overrides.Updated.Value.Ticks : entry.Updated;
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
            if (maxCapacity >= firstEmptyItemOffset)
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

            var directory = this.directoryManager.ReadDirectory(this.parentDirectoryBlockId);
            try
            {
                directory.UpdateEntry(this.index.BlockId, new DirectoryEntryInfoOverrides(null, DateTime.Now, null));
            }
            finally
            {
                this.directoryManager.UnRegisterDirectory(directory.BlockId);
            }
        }

        private int StoreName(string name)
        {
            var result = this.lastNameOffset;
            this.nameIndex.SetSizeInBlocks(Helpers.ModBaseWithCeiling(this.lastNameOffset + name.Length + 1, this.nameIndex.BlockSize));
            this.nameIndexBlockChain.Write(this.lastNameOffset, new[] { (short)name.Length }.Concat(name.Select(x => (short)x)).ToArray());
            this.nameIndex.Flush();

            this.lastNameOffset += name.Length + 1;
            return result;
        }

        internal static IDirectory ReadDirectoryUnsafe(int blockId, IDirectoryManager directoryManager)
        {
            var indexBlockProvider = new IndexBlockProvier(blockId, directoryManager.AllocationManager, directoryManager.Storage);
            var index = new Index<DirectoryItem>(indexBlockProvider, new BlockStream<int>(indexBlockProvider), directoryManager.Storage, directoryManager.AllocationManager);
            var indexStream = new BlockStream<DirectoryItem>(index);

            var buffer = new DirectoryItem[1];
            indexStream.Read(0, buffer);
            var header = buffer[0].Header;

            return new DirectoryManager(index, directoryManager, header);
        }
    }
}
