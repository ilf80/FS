using System;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;

namespace FS.Directory
{
    internal sealed class DeletionFile : IFile
    {
        private readonly int directoryBlockId;
        private IDirectoryCache directoryCache;
        private Index<byte> index;

        public DeletionFile(
           IDirectoryCache directoryCache,
           int blockId,
           int directoryBlockId)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            BlockId = blockId;
            this.directoryBlockId = directoryBlockId;

            var provider = new IndexBlockProvider(blockId, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            var indexBlockStream = new BlockStream<int>(provider);
            index = new Index<byte>(provider, indexBlockStream, this.directoryCache.AllocationManager, this.directoryCache.Storage);
        }

        public int Size => throw new InvalidOperationException("File is being deleted");

        public int BlockId { get; }

        public void Dispose()
        {
            directoryCache = null;
            index = null;
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
            var directory = directoryCache.ReadDirectory(directoryBlockId);
            try
            {
                directory.UpdateEntry(BlockId, new DirectoryEntryInfoOverrides(flags: DirectoryFlags.File | DirectoryFlags.Deleted));
            }
            finally
            {
                directoryCache.UnRegisterDirectory(directory.BlockId);
            }

            index.SetSizeInBlocks(0);
            directoryCache.AllocationManager.Release(new[] { BlockId });
            directoryCache.UnRegisterFile(BlockId);
        }
    }
}
