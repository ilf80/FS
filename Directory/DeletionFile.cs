using FS.Contracts;
using FS.Contracts.Indexes;
using System;

namespace FS.Directory
{
    internal sealed class DeletionFile : IFile
    {
        private readonly int blockId;
        private readonly int directoryBlookId;
        private IDirectoryCache directoryCache;
        private Index<byte> index;

        public DeletionFile(
           IDirectoryCache directoryCache,
           int blockId,
           int directoryBlookId,
           int size)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.blockId = blockId;
            this.directoryBlookId = directoryBlookId;

            var provider = new IndexBlockProvier(blockId, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            var indexBlockChain = new BlockStream<int>(provider);
            this.index = new Index<byte>(provider, indexBlockChain, this.directoryCache.AllocationManager, this.directoryCache.Storage);
        }

        public int Size => throw new InvalidOperationException("File is being deleted");

        public int BlockId => this.blockId;

        public void Dispose()
        {
            this.directoryCache = null;
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
            var directory = this.directoryCache.ReadDirectory(this.directoryBlookId);
            try
            {
                directory.UpdateEntry(this.blockId, new DirectoryEntryInfoOverrides(flags: DirectoryFlags.File | DirectoryFlags.Deleted));
            }
            finally
            {
                this.directoryCache.UnRegisterDirectory(directory.BlockId);
            }

            this.index.SetSizeInBlocks(0);
            this.directoryCache.AllocationManager.Release(new[] { BlockId });
            this.directoryCache.UnRegisterFile(BlockId);
        }
    }
}
