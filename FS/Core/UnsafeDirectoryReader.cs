﻿using System;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class UnsafeDirectoryReader : IUnsafeDirectoryReader
    {
        private readonly IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>> directoryBlockStreamFactory;
        private readonly IDirectoryCache directoryCache;
        private readonly IFactory<IUnsafeDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader> directoryFactory;
        private readonly IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters> directoryIndexFactory;
        private readonly IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory;

        public UnsafeDirectoryReader(
            IDirectoryCache directoryCache,
            IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory,
            IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters> directoryIndexFactory,
            IFactory<IUnsafeDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader> directoryFactory,
            IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>> directoryBlockStreamFactory)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.indexBlockProviderFactory = indexBlockProviderFactory ?? throw new ArgumentNullException(nameof(indexBlockProviderFactory));
            this.directoryIndexFactory = directoryIndexFactory ?? throw new ArgumentNullException(nameof(directoryIndexFactory));
            this.directoryFactory = directoryFactory ?? throw new ArgumentNullException(nameof(directoryFactory));
            this.directoryBlockStreamFactory = directoryBlockStreamFactory ?? throw new ArgumentNullException(nameof(directoryBlockStreamFactory));
        }

        public IUnsafeDirectory Read(int blockId)
        {
            if (blockId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockId));
            }

            var indexBlockProvider = indexBlockProviderFactory.Create(blockId, directoryCache);
            var index = directoryIndexFactory.Create(indexBlockProvider, directoryCache);
            var indexStream = directoryBlockStreamFactory.Create(index);

            var buffer = new DirectoryItem[1];
            indexStream.Read(0, buffer);
            var header = buffer[0].Header;

            return directoryFactory.Create(index, directoryCache, header);
        }
    }
}