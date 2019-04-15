using System;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class DeletionDirectory : IDeletionDirectory
    {
        private readonly IUnsafeDirectoryReader unsafeDirectoryReader;
        private IDirectoryCache directoryCache;

        public DeletionDirectory(
            IDirectoryCache directoryCache,
            int blockId,
            IFactory<IUnsafeDirectoryReader, IDirectoryCache> unsafeDirectoryReaderFactory)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            if (blockId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockId));
            }

            BlockId = blockId;

            unsafeDirectoryReader = unsafeDirectoryReaderFactory.Create(directoryCache);
        }

        public int BlockId { get; }

        public void DeleteDirectory(string name)
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public void DeleteFile(string name)
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public void Flush()
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public IDirectoryEntryInfo[] GetDirectoryEntries()
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public IDirectory OpenDirectory(string name, OpenMode openMode)
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public IFile OpenFile(string name, OpenMode openMode)
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides entry)
        {
            throw new InvalidOperationException("Directory is being deleted");
        }

        public void Dispose()
        {
            directoryCache = null;
        }

        public void Delete()
        {
            using (var directory = unsafeDirectoryReader.Read(BlockId))
            {
                if (directory.GetDirectoryEntries().Length > 0)
                {
                    UnRegister();
                    throw new InvalidOperationException("Directory is not empty");
                }

                directory.UnsafeDeleteDirectory();
                UnRegister();
            }
        }

        private void UnRegister()
        {
            directoryCache?.UnRegisterDirectory(BlockId);
        }
    }
}