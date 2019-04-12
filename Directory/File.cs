using FS.Allocattion;
using FS.Contracts;
using FS.Contracts.Indexes;
using FS.Utils;
using System;

namespace FS.Directory
{
    internal sealed class File : IFile
    {
        private readonly IDirectoryManager directoryManager;
        private readonly int blockId;
        private readonly int directoryBlookId;
        private readonly BlockStream<byte> blockChain;
        private readonly Index<byte> index;

        public File(
            IDirectoryManager directoryManager,
            int blockId,
            int directoryBlookId,
            int size)
        {
            this.directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            this.blockId = blockId;
            this.directoryBlookId = directoryBlookId;

            var provider = new IndexBlockProvier(blockId, this.directoryManager.AllocationManager, this.directoryManager.Storage);
            var indexBlockChain = new BlockStream<int>(provider);
            this.index = new Index<byte>(provider, indexBlockChain, this.directoryManager.Storage, this.directoryManager.AllocationManager);
            this.blockChain = new BlockStream<byte>(this.index);
            Size = size;
        }

        public int BlockId
        {
            get { return this.index.BlockId; }
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
            var directory = this.directoryManager.ReadDirectory(this.directoryBlookId);
            try
            {
                directory.UpdateEntry(this.blockId, new DirectoryEntryInfoOverrides(Size, DateTime.Now, null));
            }
            finally
            {
                this.directoryManager.UnRegisterDirectory(directory.BlockId);
            }
        }
    }
}
