using FS.Allocattion;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Utils;
using System;

namespace FS.Directory
{
    internal sealed class File : IFile
    {
        private readonly IBlockStorage storage;
        private readonly IAllocationManager allocationManager;
        private readonly int blockId;
        private readonly int directoryBlookId;
        private readonly BlockStream<byte> blockChain;
        private readonly Index<byte> index;

        public File(
            IBlockStorage storage,
            IAllocationManager allocationManager,
            int blockId,
            int directoryBlookId,
            int size)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.blockId = blockId;
            this.directoryBlookId = directoryBlookId;
            var provider = new IndexBlockProvier(blockId, this.allocationManager, this.storage);
            var indexBlockChain = new BlockStream<int>(provider);
            this.index = new Index<byte>(provider, indexBlockChain, this.storage, this.allocationManager);
            this.blockChain = new BlockStream<byte>(this.index);
            Size = size;
        }

        public int Size { get; private set; }
        public void Flush()
        {
            this.index.Flush();
            UpdateDirectoryEntry();
        }

        public void Read(int position, byte[] buffer)
        {
            this.blockChain.Read(position, buffer);
        }

        public void SetSize(int size)
        {
            this.index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(size, this.index.BlockSize));
            Size = size;

            UpdateDirectoryEntry();
        }

        public void Write(int position, byte[] buffer)
        {
            this.blockChain.Write(position, buffer);
        }

        private void UpdateDirectoryEntry()
        {
            using (var directory = DirectoryManager.ReadDirectory(this.directoryBlookId, this.storage, this.allocationManager))
            {
                directory.UpdateEntry(this.blockId, new DirectoryEntryInfoOverrides(Size, DateTime.Now, null));
            }
        }
    }
}
