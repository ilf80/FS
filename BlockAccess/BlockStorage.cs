using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FS.Contracts;

namespace FS.BlockAccess
{
    internal sealed class BlockStorage : IBlockStorage
    {
        private readonly string fileName;
        private readonly SemaphoreSlim lockObject = new SemaphoreSlim(1, 1);
        private FileStream fileStream;

        public long TotalSize => fileStream.Length;

        public int BlockSize => Constants.BlockSize;

        public BlockStorage(
            string fileName)
        {
            this.fileName = fileName;
        }

        public void Open()
        {
            fileStream = new FileStream(fileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                Constants.BlockSize,
                FileOptions.RandomAccess);
        }

        public void ReadBlock(int blockIndex, byte[] buffer)
        {
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
            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
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
            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
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
            fileStream.Dispose();
            lockObject.Dispose();
        }
    }
}
