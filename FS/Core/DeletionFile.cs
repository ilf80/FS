using System;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class DeletionFile : IDeletionFile
    {
        private readonly IFileParameters fileParameters;
        private IDirectoryCache directoryCache;
        private IIndex<byte> index;

        public DeletionFile(
            IDirectoryCache directoryCache,
            IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory,
            IFactory<IIndex<byte>, IIndexBlockProvider, ICommonAccessParameters> indexFactory,
            IFileParameters fileParameters)
        {
            if (indexBlockProviderFactory == null)
            {
                throw new ArgumentNullException(nameof(indexBlockProviderFactory));
            }

            if (indexFactory == null)
            {
                throw new ArgumentNullException(nameof(indexFactory));
            }

            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.fileParameters = fileParameters ?? throw new ArgumentNullException(nameof(fileParameters));

            var provider = indexBlockProviderFactory.Create(fileParameters.BlockId, directoryCache);
            index = indexFactory.Create(provider, directoryCache);
        }

        public int Size => throw new InvalidOperationException("File is being deleted");

        public int BlockId => fileParameters.BlockId;

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
            var directory = directoryCache.ReadDirectory(fileParameters.ParentDirectoryBlockId);
            try
            {
                directory.UpdateEntry(BlockId, new DirectoryEntryInfoOverrides(flags: DirectoryFlags.File | DirectoryFlags.Deleted));
            }
            finally
            {
                directoryCache.UnRegisterDirectory(directory.BlockId);
            }

            index.SetSizeInBlocks(0);
            directoryCache.AllocationManager.Release(new[] {BlockId});
            directoryCache.UnRegisterFile(BlockId);
        }
    }
}