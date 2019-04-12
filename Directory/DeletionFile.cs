using FS.Contracts;
using FS.Contracts.Indexes;
using System;

namespace FS.Directory
{
    internal sealed class DeletionFile : IFile
    {
        private readonly int blockId;
        private readonly int directoryBlookId;
        private IDirectoryCache directoryManager;
        private Index<byte> index;

        public DeletionFile(
           IDirectoryCache directoryManager,
           int blockId,
           int directoryBlookId,
           int size)
        {
            this.directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            this.blockId = blockId;
            this.directoryBlookId = directoryBlookId;

            var provider = new IndexBlockProvier(blockId, this.directoryManager.AllocationManager, this.directoryManager.Storage);
            var indexBlockChain = new BlockStream<int>(provider);
            this.index = new Index<byte>(provider, indexBlockChain, this.directoryManager.AllocationManager, this.directoryManager.Storage);
        }

        public int Size => throw new InvalidOperationException("File is being deleted");

        public int BlockId => this.blockId;

        public void Dispose()
        {
            this.directoryManager = null;
            this.index = null;
        }

        public void Flush()
        {
            throw new InvalidOperationException("File is being deleted");
        }

        public void Read(int position, byte[] buffer)
        {
            throw new InvalidOperationException("File is being deleted");
        }

        public void SetSize(int size)
        {
            throw new InvalidOperationException("File is being deleted");
        }

        public void Write(int position, byte[] buffer)
        {
            throw new InvalidOperationException("File is being deleted");
        }

        public void Delete()
        {
            var directory = this.directoryManager.ReadDirectory(this.directoryBlookId);
            try
            {
                directory.UpdateEntry(this.blockId, new DirectoryEntryInfoOverrides(flags: DirectoryFlags.File | DirectoryFlags.Deleted));
            }
            finally
            {
                this.directoryManager.UnRegisterDirectory(directory.BlockId);
            }

            this.index.SetSizeInBlocks(0);
            this.directoryManager.AllocationManager.Release(new[] { BlockId });
            this.directoryManager.UnRegisterFile(BlockId);
        }
    }
}
