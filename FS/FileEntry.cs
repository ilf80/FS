using System;
using FS.Api;
using FS.Core.Api.Directory;

namespace FS
{
    internal sealed class FileEntry : IFileEntry
    {
        private IDirectoryCache directoryCache;
        private IFile file;
        private bool isDisposed;

        public FileEntry(IDirectoryCache directoryCache, IFile file)
        {
            this.directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            this.file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public int Size => file.Size;

        public void Dispose()
        {
            if (directoryCache == null || file == null) return;

            isDisposed = true;
            directoryCache.UnRegisterFile(file.BlockId);
            directoryCache = null;
            file = null;
        }

        public void Flush()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(FileEntry));

            file.Flush();
        }

        public void Read(int position, byte[] buffer)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(FileEntry));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (position < 0 || position + buffer.Length > Size) throw new ArgumentOutOfRangeException(nameof(position));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));

            file.Read(position, buffer);
        }

        public void SetSize(int size)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(FileEntry));
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

            file.SetSize(size);
        }

        public void Write(int position, byte[] buffer)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(FileEntry));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (position < 0 || position + buffer.Length > Size) throw new ArgumentOutOfRangeException(nameof(position));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));

            file.Write(position, buffer);
        }
    }
}
