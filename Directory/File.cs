using FS.Contracts;
using FS.Contracts.Indexes;
using FS.Utils;
using System;
using System.Threading;

namespace FS.Directory
{
    internal sealed class File : IFile
    {
        private readonly IDirectoryCache directoryManager;
        private readonly int blockId;
        private readonly int directoryBlookId;
        private readonly BlockStream<byte> blockChain;
        private readonly Index<byte> index;
        private readonly ReaderWriterLockSlim lockObject = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public File(
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
            this.lockObject.EnterWriteLock();
            try
            {
                this.index.Flush();
                UpdateDirectoryEntry();
            }
            finally
            {
                this.lockObject.ExitWriteLock();
            }
        }

        public void Read(int position, byte[] buffer)
        {
            this.lockObject.EnterReadLock();
            try
            {
                this.blockChain.Read(position, buffer);
            }
            finally
            {
                this.lockObject.ExitReadLock();
            }
        }

        public void SetSize(int size)
        {
            this.lockObject.EnterWriteLock();
            try
            {
                this.index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(size, this.index.BlockSize));
                Size = size;

                UpdateDirectoryEntry();
            }
            finally
            {
                this.lockObject.ExitWriteLock();
            }
        }

        public void Write(int position, byte[] buffer)
        {
            this.lockObject.EnterWriteLock();
            try
            {
                this.blockChain.Write(position, buffer);
            }
            finally
            {
                this.lockObject.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Flush();
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
