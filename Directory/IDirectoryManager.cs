using FS.Allocattion;
using FS.BlockStorage;
using FS.Indexes;
using FS.Utils;
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

        [FieldOffset(12)]
        public int LastNameOffset;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryItem
    {
        [FieldOffset(0)]
        public DirectoryEntry Entry;

        [FieldOffset(0)]
        public DirectoryHeader Header;
    }

    internal interface IDirectory : IFlushable
    {
        IDirectory CreateDirectory(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();
    }

    public interface IDirectoryEntryInfo
    {
        bool IsDirectory { get; }

        int Size { get; }

        DateTime Created { get; }

        DateTime Updated { get; }

        string Name { get; }
    }

    internal sealed class DirectoryEntryInfo : IDirectoryEntryInfo
    {
        public DirectoryEntryInfo(DirectoryEntry header, string name)
        {
            IsDirectory = header.Flags == DirectoryFlags.Directory;
            Size = header.Size;
            Created = DateTime.FromBinary(header.Created);
            Updated = DateTime.FromBinary(header.Updated);
            Name = name;
        }

        public bool IsDirectory { get; private set; }

        public int Size { get; private set; }

        public DateTime Created { get; private set; }

        public DateTime Updated { get; private set; }

        public string Name { get; private set; }
    }

    internal class DirectoryManager: IDirectory
    {
        private readonly IIndex<DirectoryItem> index;
        private readonly IBlockStorage2 storage;
        private readonly IAllocationManager2 allocationManager;
        private readonly BlockChain<DirectoryItem> blockChain;
        private readonly Index<short> nameIndex;
        private BlockChain<short> nameIndexBlockChain;
        private int nameBlockIndex;
        private int firstEmptyItemOffset;
        private int itemsCount;
        private int lastNameOffset;

        public DirectoryManager(
            IIndex<DirectoryItem> index,
            IBlockStorage2 storage,
            IAllocationManager2 allocationManager,
            DirectoryHeader header)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.blockChain = new BlockChain<DirectoryItem>(index);
            this.nameBlockIndex = header.NameBlockIndex;

            var nameIndexProvider = new IndexBlockChainProvier(this.nameBlockIndex, this.allocationManager, this.storage);
            var nameIndexBlockChain = new BlockChain<int>(nameIndexProvider);
            this.nameIndex = new Index<short>(nameIndexProvider, nameIndexBlockChain, this.storage, this.allocationManager);
            this.nameIndexBlockChain = new BlockChain<short>(this.nameIndex);

            this.firstEmptyItemOffset = header.FirstEmptyItemOffset;
            this.itemsCount = header.ItemsCount;
            this.lastNameOffset = header.LastNameOffset;
        }

        public IDirectory CreateDirectory(string name)
        {
            var blocks = this.allocationManager.Allocate(2);

            var directoryIndexProvider = new IndexBlockChainProvier(blocks[1], this.allocationManager, this.storage);
            var directoryIndexBlockChain = new BlockChain<int>(directoryIndexProvider);
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

        public static IDirectory Read(
            int blockIndex, 
            IBlockStorage2 storage,
            IAllocationManager2 allocationManager)
        {
            var indexBlockChainProvider = new IndexBlockChainProvier(blockIndex, allocationManager, storage);
            var index = new Index<DirectoryItem>(indexBlockChainProvider, new BlockChain<int>(indexBlockChainProvider), storage, allocationManager);
            var indexBlockChain = new BlockChain<DirectoryItem>(index);

            var buffer = new DirectoryItem[1];
            indexBlockChain.Read(0, buffer);
            var header = buffer[0].Header;

            return new DirectoryManager(index, storage, allocationManager, header);
        }

        public void Flush()
        {
            this.index.Flush();
            this.nameIndex.Flush();
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
