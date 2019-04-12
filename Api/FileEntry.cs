using FS.Directory;
using System;

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

        public int Size => this.file.Size;

        public void Dispose()
        {
            if (this.directoryCache != null && this.file != null)
            {
                this.directoryCache.UnRegisterFile(this.file.BlockId);
                this.directoryCache = null;
                this.file = null;
            }
        }

        public void Flush()
        {
            this.file.Flush();
        }

        public void Read(int position, byte[] buffer)
        {
            this.file.Read(position, buffer);
        }

        public void SetSize(int size)
        {
            this.file.SetSize(size);
        }

        public void Write(int position, byte[] buffer)
        {
            this.file.Write(position, buffer);
        }
    }
}
