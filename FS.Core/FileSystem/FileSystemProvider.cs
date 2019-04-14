using System;
using FS.Api;
using FS.Api.Container;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Api.Common;
using FS.Core.Api.Directory;
using FS.Core.Api.FileSystem;
using FS.Core.Directory;

namespace FS.Core.FileSystem
{
    internal sealed class FileSystemProvider : IFileSystemProvider
    {
        private readonly IFactory<IBlockStorage, string> blockStorageFactory;
        private readonly IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory;
        private readonly IFactory<IIndex<int>, IIndexBlockProvider, ICommonAccessParameters> indexFactory;
        private readonly IFactory<IAllocationManager, IFactory<IIndex<int>, IAllocationManager>, IBlockStorage, int> allocationManagerFactory;
        private readonly IFactory<IDirectoryCache, ICommonAccessParameters> directoryCacheFactory;
        private const int AllocationBlockId = 1;
        private const int RootDirectoryBlockId = 2;
        private const int RootDirectoryDataBlockId = 3;
        private const int NameBlockIndex = 4;

        private bool isOpened;
        private bool isDisposed;
        private IAllocationManager allocationManager;
        private IBlockStorage storage;

        public FileSystemProvider(
            IFactory<IBlockStorage, string> blockStorageFactory,
            IFactory<IIndexBlockProvider, int, ICommonAccessParameters> indexBlockProviderFactory,
            IFactory<IIndex<int>, IIndexBlockProvider, ICommonAccessParameters> indexFactory,
            IFactory<IAllocationManager, IFactory<IIndex<int>, IAllocationManager>, IBlockStorage, int> allocationManagerFactory,
            IFactory<IDirectoryCache, ICommonAccessParameters> directoryCacheFactory)
        {
            this.blockStorageFactory = blockStorageFactory ?? throw new ArgumentNullException(nameof(blockStorageFactory));
            this.indexBlockProviderFactory = indexBlockProviderFactory ?? throw new ArgumentNullException(nameof(indexBlockProviderFactory));
            this.indexFactory = indexFactory ?? throw new ArgumentNullException(nameof(indexFactory));
            this.allocationManagerFactory = allocationManagerFactory ?? throw new ArgumentNullException(nameof(allocationManagerFactory));
            this.directoryCacheFactory = directoryCacheFactory ?? throw new ArgumentNullException(nameof(directoryCacheFactory));
        }

        public void Dispose()
        {
            if (!isDisposed && isOpened)
            {
                Flush();
            }

            isDisposed = true;

            DirectoryCache?.Dispose();
            storage.Dispose();

            DirectoryCache = null;
            allocationManager = null;
            storage = null;
        }

        public IDirectoryCache DirectoryCache { get; private set; }

        public IDirectory RootDirectory { get; private set; }

        public void Open(string fileName, OpenMode openMode)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (isOpened) throw new InvalidOperationException("File system is already opened");

            storage = blockStorageFactory.Create(fileName);
            storage.Open(openMode);

            try
            {
                FSHeader header;
                if (openMode == OpenMode.Create)
                {
                    header = CreateFile();
                }
                else
                {
                    var headers = new FSHeader[1];
                    storage.ReadBlock(0, headers);
                    header = headers[0];
                }

                InitializeFromHeader(storage, header);
            }
            catch
            {
                storage.Dispose();
            }

            isOpened = true;
        }

        public void Flush()
        {
            if (!isOpened) throw new InvalidOperationException("provider is not initialized");
            if (isDisposed) throw new ObjectDisposedException(nameof(FileSystemProvider));

            allocationManager.Flush();
            WriteHeader();
        }

        private void InitializeFromHeader(IBlockStorage storage, FSHeader header)
        {
            var allocationIndexFactory = new GenericFactory<IIndex<int>, IAllocationManager>(
                m =>
                {
                    var accessParameters = new CommonAccessParameters(storage, m);
                    var allocationIndexProvider =
                        indexBlockProviderFactory.Create(header.AllocationBlock, accessParameters);
                    return indexFactory.Create(allocationIndexProvider, accessParameters);
                });

            allocationManager = allocationManagerFactory.Create(allocationIndexFactory, storage, header.FreeBlockCount);

            var dictionaryCache = directoryCacheFactory.Create(new CommonAccessParameters(storage, allocationManager));
            var rootDirectory = dictionaryCache.ReadDirectory(header.RootDirectoryBlock);

            RootDirectory = rootDirectory;
            DirectoryCache = dictionaryCache;
        }

        private FSHeader CreateFile()
        {
            var buffer = new byte[512];
            var fsHeader = new FSHeader
            {
                AllocationBlock = AllocationBlockId,
                RootDirectoryBlock = RootDirectoryBlockId,
                FreeBlockCount = 0
            };
            storage.WriteBlock(0, new[] { fsHeader });
            storage.WriteBlock(1, buffer);

            buffer[0] = RootDirectoryDataBlockId;
            storage.WriteBlock(2, buffer);

            var fsRoot = new DirectoryHeader
            {
                FirstEmptyItemOffset = 1,
                ItemsCount = 0,
                LastNameOffset = 0,
                NameBlockIndex = NameBlockIndex,
                ParentDirectoryBlockIndex = RootDirectoryBlockId
            };
            storage.WriteBlock(RootDirectoryDataBlockId, new[] { new DirectoryHeaderRoot { Header = fsRoot } });

            buffer[0] = 0;
            storage.WriteBlock(NameBlockIndex, buffer);

            return fsHeader;
        }

        private void WriteHeader()
        {
            var fsHeader = new FSHeader
            {
                AllocationBlock = allocationManager.BlockId,
                RootDirectoryBlock = RootDirectory.BlockId,
                FreeBlockCount = allocationManager.ReleasedBlockCount
            };
            storage.WriteBlock(0, new[] { fsHeader });
        }
    }
}
