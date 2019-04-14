using System;
using FS.Directory;

namespace FS.Api
{
    public sealed class FileEntry : IFileEntry
    {
        private IDirectoryCache directoryCache;
        private IFile file;

        internal FileEntry(IDirectoryCache directoryCache, IFile file)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public int Size => file.Size;

        public void Dispose()
        {
            if (directoryCache == null || file == null) return;

            directoryCache.UnRegisterFile(file.BlockId);
            directoryCache = null;
            file = null;
        }

        public void Flush()
        {
            file.Flush();
        }

        public void Read(int position, byte[] buffer)
        {
            file.Read(position, buffer);
        }

        public void SetSize(int size)
        {
            file.SetSize(size);
        }

        public void Write(int position, byte[] buffer)
        {
            file.Write(position, buffer);
        }
    }
}
