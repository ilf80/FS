using System;
using System.Threading;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Utils;

namespace FS.Directory
{
    internal sealed class File : IFile
    {
        private readonly IDirectoryCache directoryCache;
        private readonly int blockId;
        private readonly int directoryBlockId;
        private readonly BlockStream<byte> blockStream;
        private readonly Index<byte> index;
        private readonly ReaderWriterLockSlim lockObject = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public File(
            IDirectoryCache directoryCache,
            int blockId,
            int directoryBlockId,
            int size)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.blockId = blockId;
            this.directoryBlockId = directoryBlockId;

            var provider = new IndexBlockProvider(blockId, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            var indexBlockStream = new BlockStream<int>(provider);
            index = new Index<byte>(provider, indexBlockStream, this.directoryCache.AllocationManager, this.directoryCache.Storage);
            blockStream = new BlockStream<byte>(index);
            Size = size;
        }

        public int BlockId => index.BlockId;

        public int Size { get; private set; }

        public void Flush()
        {
            lockObject.EnterWriteLock();
            try
            {
                index.Flush();
                UpdateDirectoryEntry();
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        public void Read(int position, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (position < 0)
            {
                throw new ArgumentException("position cannot me negative");
            }
            if (position + buffer.Length > Size)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Out of file bounds");
            }

            lockObject.EnterReadLock();
            try
            {
                blockStream.Read(position, buffer);
            }
            finally
            {
                lockObject.ExitReadLock();
            }
        }

        public void SetSize(int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            lockObject.EnterWriteLock();
            try
            {
                index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(size, index.BlockSize));
                Size = size;

                UpdateDirectoryEntry();
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        public void Write(int position, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (position < 0)
            {
                throw new ArgumentException("position cannot me negative");
            }
            if (position + buffer.Length > Size)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Out of file bounds");
            }

            lockObject.EnterWriteLock();
            try
            {
                blockStream.Write(position, buffer);
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Flush();
            lockObject.Dispose();
        }

        private void UpdateDirectoryEntry()
        {
            var directory = directoryCache.ReadDirectory(directoryBlockId);
            try
            {
                directory.UpdateEntry(blockId, new DirectoryEntryInfoOverrides(Size, DateTime.Now));
            }
            finally
            {
                directoryCache.UnRegisterDirectory(directory.BlockId);
            }
        }
    }
}
