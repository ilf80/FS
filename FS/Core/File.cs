using System;
using System.Threading;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class File : IFile
    {
        private readonly int blockId;
        private readonly IBlockStream<byte> blockStream;
        private readonly int directoryBlockId;
        private readonly IDirectoryCache directoryCache;
        private readonly IIndex<byte> index;
        private readonly ReaderWriterLockSlim lockObject = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public File(
            IDirectoryCache directoryCache,
            IFileParameters fileParameters,
            IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory,
            IFactory<IIndex<byte>, IIndexBlockProvider, ICommonAccessParameters> indexFactory,
            IFactory<IBlockStream<byte>, IBlockProvider<byte>> blockStreamFactory)
        {
            if (fileParameters == null)
            {
                throw new ArgumentNullException(nameof(fileParameters));
            }

            if (indexBlockProviderFactory == null)
            {
                throw new ArgumentNullException(nameof(indexBlockProviderFactory));
            }

            if (blockStreamFactory == null)
            {
                throw new ArgumentNullException(nameof(blockStreamFactory));
            }

            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));

            blockId = fileParameters.BlockId;
            directoryBlockId = fileParameters.ParentDirectoryBlockId;
            Size = fileParameters.Size;

            var provider = indexBlockProviderFactory.Create(blockId, this.directoryCache);
            index = indexFactory.Create(provider, directoryCache);
            blockStream = blockStreamFactory.Create(index);
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
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (position < 0 || position + buffer.Length > Size)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
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
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

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
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (position < 0 || position + buffer.Length > Size)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
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