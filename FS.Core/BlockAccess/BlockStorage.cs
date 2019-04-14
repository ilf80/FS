using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FS.Api;
using FS.Core.Api.BlockAccess;
using FS.Core.Contracts;

namespace FS.Core.BlockAccess
{
    internal sealed class BlockStorage : IBlockStorage
    {
        private readonly string fileName;
        private readonly SemaphoreSlim lockObject = new SemaphoreSlim(1, 1);
        private FileStream fileStream;
        private bool isDisposed;
        private bool isOpened;

        public long TotalSize => fileStream.Length;

        public int BlockSize => Constants.BlockSize;

        public BlockStorage(string fileName)
        {
            this.fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public void Open(OpenMode mode)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BlockStorage));

            var fileMode = GetFileMode(mode);
            fileStream = new FileStream(fileName,
                fileMode,
                FileAccess.ReadWrite,
                FileShare.Read,
                Constants.BlockSize,
                FileOptions.RandomAccess);
            isOpened = true;
        }

        public void ReadBlock(int blockIndex, byte[] buffer)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BlockStorage));
            if (!isOpened) throw new InvalidOperationException("storage is not opened");
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (blockIndex < 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            lockObject.Wait();
            try
            {
                fileStream.Position = blockIndex * Constants.BlockSize;
                fileStream.Read(buffer, 0, Constants.BlockSize);
            }
            finally
            {
                lockObject.Release();
            }
        }

        public void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BlockStorage));
            if (!isOpened) throw new InvalidOperationException("storage is not opened");
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (blockIndex < 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                lockObject.Wait();
                try
                {
                    fileStream.Position = blockIndex * Constants.BlockSize;
                    fileStream.Read(tempBuffer, 0, Constants.BlockSize);
                }
                finally
                {
                    lockObject.Release();
                }

                Marshal.Copy(tempBuffer, 0, handle.AddrOfPinnedObject(), tempBuffer.Length);
            }
            finally
            {
                handle.Free();
            }
        }

        public void WriteBlock(int blockIndex, byte[] buffer)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BlockStorage));
            if (!isOpened) throw new InvalidOperationException("storage is not opened");
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (blockIndex < 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            lockObject.Wait();
            try
            {
                fileStream.Position = blockIndex * Constants.BlockSize;
                fileStream.Write(buffer, 0, Constants.BlockSize);
            }
            finally
            {
                lockObject.Release();
            }
        }

        public void WriteBlock<T>(int blockIndex, T[] buffer) where T : struct
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BlockStorage));
            if (!isOpened) throw new InvalidOperationException("storage is not opened");
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (blockIndex < 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject(), tempBuffer, 0, tempBuffer.Length);
            }
            finally
            {
                handle.Free();
            }

            lockObject.Wait();
            try
            {
                fileStream.Position = blockIndex * Constants.BlockSize;
                fileStream.Write(tempBuffer, 0, Constants.BlockSize);
            }
            finally
            {
                lockObject.Release();
            }
        }

        public int[] Extend(int blockCount)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BlockStorage));
            if (!isOpened) throw new InvalidOperationException("storage is not opened");
            if (blockCount <= 0) throw new ArgumentOutOfRangeException(nameof(blockCount));

            lockObject.Wait();
            try
            {
                var length = (int)fileStream.Length;
                var result = Enumerable.Range(length / BlockSize + 1, blockCount).ToArray();
                fileStream.SetLength(length + blockCount * BlockSize);
                return result;
            }
            finally
            {
                lockObject.Release();
            }
        }

        public void Dispose()
        {
            isDisposed = true;
            fileStream?.Dispose();
            lockObject.Dispose();
        }

        private static FileMode GetFileMode(OpenMode mode)
        {
            switch(mode)
            {
                case OpenMode.OpenExisting:
                    return FileMode.Open;
                case OpenMode.OpenOrCreate:
                    return FileMode.OpenOrCreate;
                case OpenMode.Create:
                    return FileMode.CreateNew;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}
