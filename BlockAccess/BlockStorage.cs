using FS.BlockAccess;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FS.BlockAccess
{
    internal class BlockStorage : IBlockStorage
    {
        private readonly string fileName;
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
            this.fileStream.Position = blockIndex * Constants.BlockSize;
            this.fileStream.Read(buffer, 0, Constants.BlockSize);
        }

        public void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct
        {
            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                this.fileStream.Position = blockIndex * Constants.BlockSize;
                this.fileStream.Read(tempBuffer, 0, Constants.BlockSize);

                Marshal.Copy(tempBuffer, 0, handle.AddrOfPinnedObject(), tempBuffer.Length);
            }
            finally
            {
                handle.Free();
            }
        }

        public void WriteBlock(int blockIndex, byte[] buffer)
        {
            this.fileStream.Position = blockIndex * Constants.BlockSize;
            this.fileStream.Write(buffer, 0, Constants.BlockSize);
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

            this.fileStream.Position = blockIndex * Constants.BlockSize;
            this.fileStream.Write(tempBuffer, 0, Constants.BlockSize);
        }

        public int[] Extend(int blockCount)
        {
            var result = Enumerable.Range((int)(this.fileStream.Length / BlockSize) + 1, blockCount).ToArray();
            this.fileStream.SetLength(this.fileStream.Length + blockCount * BlockSize);
            return result;
        }

        public void Dispose()
        {
            this.fileStream.Dispose();
        }
    }
}
