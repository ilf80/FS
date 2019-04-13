using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Utils;
using System;
using System.Threading;

namespace FS.Directory
{
    internal sealed class File : IFile
    {
        private readonly IDirectoryCache directoryCache;
        private readonly int blockId;
        private readonly int directoryBlookId;
        private readonly BlockStream<byte> blockStream;
        private readonly Index<byte> index;
        private readonly ReaderWriterLockSlim lockObject = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public File(
            IDirectoryCache directoryCache,
            int blockId,
            int directoryBlookId,
            int size)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.blockId = blockId;
            this.directoryBlookId = directoryBlookId;

            var provider = new IndexBlockProvier(blockId, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            var indexBlockStream = new BlockStream<int>(provider);
            this.index = new Index<byte>(provider, indexBlockStream, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            this.blockStream = new BlockStream<byte>(this.index);
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
            CheckPosition(position, buffer.Length);

            this.lockObject.EnterReadLock();
            try
            {
                this.blockStream.Read(position, buffer);
            }
            finally
            {
                this.lockObject.ExitReadLock();
            }
        }

        public void SetSize(int size)
        {
            CheckSize(size);

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
            CheckPosition(position, buffer.Length);

            this.lockObject.EnterWriteLock();
            try
            {
                this.blockStream.Write(position, buffer);
            }
            finally
            {
                this.lockObject.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Flush();
            this.lockObject.Dispose();
        }

        private void UpdateDirectoryEntry()
        {
            var directory = this.directoryCache.ReadDirectory(this.directoryBlookId);
            try
            {
                directory.UpdateEntry(this.blockId, new DirectoryEntryInfoOverrides(Size, DateTime.Now));
            }
            finally
            {
                this.directoryCache.UnRegisterDirectory(directory.BlockId);
            }
        }

        private void CheckSize(int size)
        {
            if (size < 0)
            {
                throw new ArgumentException("File size size be negative");
            }
        }

        private void CheckPosition(int position, int length)
        {
            if (position < 0)
            {
                throw new ArgumentException("position cannot me negative");
            }
            if (position + length > Size)
            {
                throw new ArgumentOutOfRangeException("Out of file bounds");
            }
        }
    }
}
