using FS.Allocattion;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FS.Directory
{
    internal class DirectoryManager: IDirectory
    {
        private readonly IIndex<DirectoryItem> index;
        private readonly IBlockStorage storage;
        private readonly IAllocationManager allocationManager;
        private readonly BlockStream<DirectoryItem> blockChain;
        private readonly Index<short> nameIndex;
        private BlockStream<short> nameIndexBlockChain;
        private int nameBlockIndex;
        private int firstEmptyItemOffset;
        private int itemsCount;
        private int lastNameOffset;

        public DirectoryManager(
            IIndex<DirectoryItem> index,
            IBlockStorage storage,
            IAllocationManager allocationManager,
            DirectoryHeader header)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.blockChain = new BlockStream<DirectoryItem>(index);
            this.nameBlockIndex = header.NameBlockIndex;

            var nameIndexProvider = new IndexBlockProvier(this.nameBlockIndex, this.allocationManager, this.storage);
            var nameIndexBlockChain = new BlockStream<int>(nameIndexProvider);
            this.nameIndex = new Index<short>(nameIndexProvider, nameIndexBlockChain, this.storage, this.allocationManager);
            this.nameIndexBlockChain = new BlockStream<short>(this.nameIndex);

            this.firstEmptyItemOffset = header.FirstEmptyItemOffset;
            this.itemsCount = header.ItemsCount;
            this.lastNameOffset = header.LastNameOffset;
        }

        public IDirectory OpenDirectory(string name)
        {
            var entries = GetDirectoryEntries();
            var entry = entries.FirstOrDefault(x => x.Name == name);
            if (entry != null)
            {
                return ReadDirectory(entry.BlockId, this.storage, this.allocationManager);
            }

            var blocks = this.allocationManager.Allocate(2);

            var directoryIndexProvider = new IndexBlockProvier(blocks[1], this.allocationManager, this.storage);
            var directoryIndexBlockChain = new BlockStream<int>(directoryIndexProvider);
            var directoryIndex = new Index<DirectoryItem>(directoryIndexProvider, directoryIndexBlockChain, this.storage, this.allocationManager);
            directoryIndex.SetSizeInBlocks(1);
            directoryIndex.Flush();

            var header = new DirectoryHeader
            {
                FirstEmptyItemOffset = 1,
                ItemsCount = 0,
                LastNameOffset = 0,
                NameBlockIndex = blocks[0]
            };
            var directory = new DirectoryManager(directoryIndex, this.storage, this.allocationManager, header);
            directory.UpdateHeader();

            AddEntry(blocks[0], name, DirectoryFlags.Directory);


            return directory;
        }

        public IDirectoryEntryInfo[] GetDirectoryEntries()
        {
            var buffer = new DirectoryItem[this.itemsCount];
            this.blockChain.Read(1, buffer);

            var names = new short[this.lastNameOffset];
            this.nameIndexBlockChain.Read(0, names);

            var result = new List<IDirectoryEntryInfo>(this.itemsCount);
            foreach(var item in buffer)
            {
                var entry = item.Entry;

                var nameLength = names[entry.NameOffset];
                var nameBuffer = new char[nameLength];
                for(var i = 0; i < nameLength; i++)
                {
                    nameBuffer[i] = (char)names[entry.NameOffset + 1 + i];
                }

                result.Add(new DirectoryEntryInfo(entry, new string(nameBuffer)));
            }
            return result.ToArray();
        }

        public IFile OpenFile(string name)
        {
            var entries = GetDirectoryEntries();
            var entry = entries.FirstOrDefault(x => x.Name == name);
            if (entry != null)
            {
                return new File(this.storage, this.allocationManager, entry.BlockId, this.index.BlockId, entry.Size);
            }

            var blocks = this.allocationManager.Allocate(1);
            AddEntry(blocks[0], name, DirectoryFlags.File);

            var result = new File(this.storage, this.allocationManager, blocks[0], this.index.BlockId, 0);
            result.SetSize(1);
            result.Flush();

            return result;
        }

        public static IDirectory ReadDirectory(
            int blockIndex, 
            IBlockStorage storage,
            IAllocationManager allocationManager)
        {
            var indexBlockProvider = new IndexBlockProvier(blockIndex, allocationManager, storage);
            var index = new Index<DirectoryItem>(indexBlockProvider, new BlockStream<int>(indexBlockProvider), storage, allocationManager);
            var indexStream = new BlockStream<DirectoryItem>(index);

            var buffer = new DirectoryItem[1];
            indexStream.Read(0, buffer);
            var header = buffer[0].Header;

            return new DirectoryManager(index, storage, allocationManager, header);
        }

        public void Flush()
        {
            this.index.Flush();
            this.nameIndex.Flush();
        }

        public void Dispose()
        {
        }

        public void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides overrides)
        {
            var buffer = new DirectoryItem[this.itemsCount];
            this.blockChain.Read(1, buffer);

            for(var i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                if (entry.Entry.BlockIndex == blockId)
                {
                    ApplyOverrides(ref entry.Entry, overrides);
                    this.blockChain.Write(i + 1, new[] { entry });
                    return;
                }
            }

            throw new Exception("Entry not found");
        }

        private void ApplyOverrides(ref DirectoryEntry entry, IDirectoryEntryInfoOverrides overrides)
        {
            entry.Size = overrides.Size ?? entry.Size;
            entry.Updated = overrides.Updated.HasValue ? overrides.Updated.Value.Ticks : entry.Updated;
        }

        private void AddEntry(int blockId, string name, DirectoryFlags flags)
        {
            var directoryEntryItem = new DirectoryItem
            {
                Entry = new DirectoryEntry
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
        private void AddEntry(DirectoryItem directoryEntryItem)
        {
            this.blockChain.Write(this.firstEmptyItemOffset, new[] { directoryEntryItem });

            this.firstEmptyItemOffset = FindEmptyItem(this.firstEmptyItemOffset);
            this.itemsCount++;

            UpdateHeader();
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
                this.blockChain.Read(firstEmptyItemOffset, buffer);

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
                    LastNameOffset = this.lastNameOffset
                }
            };
            this.blockChain.Write(0, new[] { directoryHeaderItem });
        }

        private void UpdateAccessTime()
        {
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
    }
}
