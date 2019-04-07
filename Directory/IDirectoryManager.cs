using FS.Allocattion;
using FS.BlockStorage;
using FS.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FS.Directory
{
    [Flags]
    internal enum DirectoryFlags : uint
    {
        None = 0,
        File = 0b0001,
        Directory = 0b0010,
        Deleted = 0b1000
    }


    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryEntry
    {
        [FieldOffset(0)]
        public DirectoryFlags Flags;

        [FieldOffset(4)]
        public int Size;

        [FieldOffset(8)]
        public long Created;

        [FieldOffset(16)]
        public long Updated;

        [FieldOffset(24)]
        public int NameOffset;

        [FieldOffset(28)]
        public int BlockIndex;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryHeader
    {
        [FieldOffset(0)]
        public int NameBlockIndex;

        [FieldOffset(4)]
        public int FirstEmptyItemOffset;

        [FieldOffset(8)]
        public int ItemsCount;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryItem
    {
        [FieldOffset(0)]
        public DirectoryEntry Entry;

        [FieldOffset(0)]
        public DirectoryHeader Header;
    }

    internal interface IDirectory
    {

    }

    internal class Directory: IDirectory
    {
        private readonly IIndex<DirectoryItem> index;
        private readonly IBlockStorage2 storage;
        private readonly IAllocationManager2 allocationManager;
        private readonly BlockChain<DirectoryItem> blockChain;

        private int nameBlockIndex;
        private int firstEmptyItemOffset;
        private int itemsCount;

        public Directory(
            IIndex<DirectoryItem> index,
            IBlockStorage2 storage,
            IAllocationManager2 allocationManager,
            int nameBlockIndex,
            int firstEmptyItemOffset)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.blockChain = new BlockChain<DirectoryItem>(index);
            this.nameBlockIndex = nameBlockIndex;
            this.firstEmptyItemOffset = firstEmptyItemOffset;
        }

        public IDirectory CreateDirectory(string name)
        {
            var blocks = this.allocationManager.Allocate(2);

            var directoryIndexProvider = new IndexBlockChainProvier(blocks[1], this.allocationManager, this.storage);
            var directoryIndexBlockChain = new BlockChain<int>(directoryIndexProvider);
            var directoryIndex = new Index<DirectoryItem>(directoryIndexProvider, directoryIndexBlockChain, this.storage, this.allocationManager);

            var directory = new Directory(directoryIndex, this.storage, this.allocationManager, blocks[0], 1);
            directory.UpdateHeader();

            AddEntry(blocks[0], name, DirectoryFlags.Directory);


            return directory;
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
            var buffer = new DirectoryItem[this.index.SizeInBlocks * this.index.BlockSize - firstEmptyItemOffset];
            this.index.Read(firstEmptyItemOffset, buffer);

            var emptyEntryindex = buffer
                .Where(x => x.Entry.Flags == DirectoryFlags.None || (x.Entry.Flags & DirectoryFlags.Deleted) != 0)
                .Select((x, index) => (int?)index)
                .FirstOrDefault();

            if (emptyEntryindex == null)
            {
                this.index.SetSizeInBlocks(this.index.SizeInBlocks + 1);
                return firstEmptyItemOffset + buffer.Length;
            }
            return emptyEntryindex.Value;
        }

        private void UpdateHeader()
        {
            var directoryHeaderItem = new DirectoryItem
            {
                Header = new DirectoryHeader
                {
                    FirstEmptyItemOffset = this.firstEmptyItemOffset,
                    NameBlockIndex = this.nameBlockIndex,
                    ItemsCount = this.itemsCount
                }
            };
            this.blockChain.Write(0, new[] { directoryHeaderItem });
        }

        private void UpdateAccessTime()
        {
        }

        private int StoreName(string name)
        {
            return 0;
        }
    }
}
