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
        private readonly BlockStream<byte> blockChain;
        private readonly Index<byte> index;

        public File(
            IBlockStorage storage,
            IAllocationManager allocationManager,
            int blockId)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.blockId = blockId;

            var provider = new IndexBlockProvier(blockId, this.allocationManager, this.storage);
            var indexBlockChain = new BlockStream<int>(provider);
            this.index = new Index<byte>(provider, indexBlockChain, this.storage, this.allocationManager);
            this.blockChain = new BlockStream<byte>(this.index);
        }
        public void Flush()
        {
            this.index.Flush();
        }

        public void Read(int position, byte[] buffer)
        {
            this.blockChain.Read(position, buffer);
        }

        public void SetSize(int size)
        {
            this.index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(size, this.index.BlockSize));
        }

        public void Write(int position, byte[] buffer)
        {
            this.blockChain.Write(position, buffer);
        }
    }
}
