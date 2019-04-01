using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading.Tasks;
using FS.Contracts;

namespace FS.BlockStorage
{
    internal sealed class BlockDevice : IBlockStorage
    {
        private readonly string fileName;
        private readonly TaskFactory taskFactory;
        private FileStream fileStream;

        public uint TotalSize => throw new NotImplementedException();

        public int BlockSize => Constants.BlockSize;

        public BlockDevice(
            string fileName,
            TaskFactory taskFactory)
        {
            this.fileName = fileName;
            this.taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
        }

        public void Open()
        {
            this.fileStream = new FileStream(this.fileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                Constants.BlockSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess);
        }

        public Task ReadBlock(uint blockIndex, byte[] buffer)
        {
            this.fileStream.Position = blockIndex * Constants.BlockSize;
            var asyncResult = this.fileStream.BeginRead(buffer, 0, Constants.BlockSize, r => { }, null);
            return this.taskFactory.FromAsync(asyncResult, r => { });
        }

        public Task ReadBlock<T>(uint blockIndex, out T target) where T : struct
        {
            target = default(T);
            var buffer = new byte[Marshal.SizeOf<T>()];
            GCHandle handle = GCHandle.Alloc(target, GCHandleType.Pinned);
            return ReadBlock(blockIndex, buffer)
                .ContinueWith(x =>
                {
                    Marshal.Copy(buffer, 0, handle.AddrOfPinnedObject(), buffer.Length);
                })
                .ContinueWith(x =>
                    handle.Free(),
                    TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously
                );
        }

        public Task<T> ReadBlock<T>(uint blockIndex) where T : struct
        {
            var buffer = new byte[Marshal.SizeOf<T>()];
            return ReadBlock(blockIndex, buffer)
                .ContinueWith(x =>
                {
                    return BytesToStruct<T>(ref buffer);
                });
        }

        public Task WriteBlock(uint blockIndex, byte[] buffer)
        {
            this.fileStream.Position = blockIndex * Constants.BlockSize;
            var asyncResult = this.fileStream.BeginWrite(buffer, 0, Constants.BlockSize, null, null);
            return this.taskFactory.FromAsync(asyncResult, r => { });
        }

        public Task WriteBlock<T>(uint blockIndex, ref T target) where T : struct
        {
            var buffer = StructToBytes(target);
            return WriteBlock(blockIndex, buffer);
        }

        public void Dispose()
        {
            this.fileStream.Dispose();
        }

        private static T BytesToStruct<T>(ref byte[] rawData) where T : struct
        {
            T result = default(T);
            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                result = (T)Marshal.PtrToStructure(rawDataPtr, typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return result;
        }

        private static byte[] StructToBytes<T>(T data) where T : struct
        {
            byte[] rawData = new byte[Marshal.SizeOf(data)];
            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(data, rawDataPtr, false);
            }
            finally
            {
                handle.Free();
            }
            return rawData;
        }
    }
}
