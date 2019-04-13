using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace FS.Contracts
{
    internal class BlockStorage : IBlockStorage
    {
        private readonly string fileName;
        private readonly SemaphoreSlim lockObject = new SemaphoreSlim(1, 1);
        private FileStream fileStream;

        public long TotalSize => this.fileStream.Length;

        public int BlockSize => Constants.BlockSize;

        public BlockStorage(
            string fileName)
        {
            this.fileName = fileName;
        }

        public void Open()
        {
            this.fileStream = new FileStream(this.fileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                //FileShare.None,
                FileShare.Read,
                Constants.BlockSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess);
        }

        public void ReadBlock(int blockIndex, byte[] buffer)
        {
            this.lockObject.Wait();
            try
            {
                this.fileStream.Position = blockIndex * Constants.BlockSize;
                this.fileStream.Read(buffer, 0, Constants.BlockSize);
            }
            finally
            {
                this.lockObject.Release();
            }
        }

        public void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct
        {
            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                this.lockObject.Wait();
                try
                {
                    this.fileStream.Position = blockIndex * Constants.BlockSize;
                    this.fileStream.Read(tempBuffer, 0, Constants.BlockSize);
                }
                finally
                {
                    this.lockObject.Release();
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
            this.lockObject.Wait();
            try
            {
                this.fileStream.Position = blockIndex * Constants.BlockSize;
                this.fileStream.Write(buffer, 0, Constants.BlockSize);
            }
            finally
            {
                this.lockObject.Release();
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

            this.lockObject.Wait();
            try
            {
                this.fileStream.Position = blockIndex * Constants.BlockSize;
                this.fileStream.Write(tempBuffer, 0, Constants.BlockSize);
            }
            finally
            {
                this.lockObject.Release();
            }
        }

        public int[] Extend(int blockCount)
        {
            this.lockObject.Wait();
            try
            {
                var length = (int)this.fileStream.Length;
                var result = Enumerable.Range(length / BlockSize + 1, blockCount).ToArray();
                this.fileStream.SetLength(length + blockCount * BlockSize);
                return result;
            }
            finally
            {
                this.lockObject.Release();
            }
        }

        public void Dispose()
        {
            this.fileStream.Dispose();
            this.lockObject.Dispose();
        }
    }
}
