using System;
using FS.Api;
using FS.Core.Allocation;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Api.Directory;
using FS.Core.Api.FileSystem;
using FS.Core.BlockAccess;
using FS.Core.BlockAccess.Indexes;
using FS.Core.Directory;

namespace FS.Core.FileSystem
{
    internal sealed class FileSystemProvider : IFileSystemProvider
    {
        private const int AllocationBlockId = 1;
        private const int RootDirectoryBlockId = 2;
        private const int RootDirectoryDataBlockId = 3;
        private const int NameBlockIndex = 4;

        private bool isOpened;
        private bool isDisposed;
        private AllocationManager allocationManager;
        private IBlockStorage storage;

        public FileSystemProvider()
        {   
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

            storage = new BlockStorage(fileName);
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
            IIndex<int> AllocationIndexFactory(IAllocationManager m)
            {
                var allocationIndexProvider = new IndexBlockProvider(header.AllocationBlock, m, storage);
                return new Index<int>(allocationIndexProvider, new BlockStream<int>(allocationIndexProvider), m, storage);
            }

            allocationManager = new AllocationManager(AllocationIndexFactory, storage, header.FreeBlockCount);

            var dictionaryCache = new DirectoryCache(storage, allocationManager);
            var rootDirectory = dictionaryCache.ReadDirectory(header.RootDirectoryBlock);

            RootDirectory = rootDirectory;
            DirectoryCache = DirectoryCache;
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
