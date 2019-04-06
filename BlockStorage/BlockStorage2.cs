using FS.Contracts;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FS.BlockStorage
{
    internal class BlockStorage2 : IBlockStorage2
    {
        private readonly string fileName;
        private FileStream fileStream;

        public uint TotalSize => throw new NotImplementedException();

        public int BlockSize => Constants.BlockSize;

        public BlockStorage2(
            string fileName)
        {
            this.fileName = fileName;
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

        public void ReadBlock(int blockIndex, byte[] buffer)
        {
            this.fileStream.Position = blockIndex * Constants.BlockSize;
            this.fileStream.Read(buffer, 0, Constants.BlockSize);
        }

        public void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct
        {
            var tempBuffer = new byte[Marshal.SizeOf<T>() * buffer.Length];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            this.fileStream.Position = blockIndex * Constants.BlockSize;
            this.fileStream.Read(tempBuffer, 0, Constants.BlockSize);

            Marshal.Copy(tempBuffer, 0, handle.AddrOfPinnedObject(), tempBuffer.Length);
            handle.Free();
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
            Marshal.Copy(handle.AddrOfPinnedObject(), tempBuffer, 0, tempBuffer.Length);
            handle.Free();

            this.fileStream.Position = blockIndex * Constants.BlockSize;
            this.fileStream.Write(tempBuffer, 0, Constants.BlockSize);
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


    //internal interface IDirectoryManager
    //{
    //    IEnumerable<IDirectoryEntry> ReadRoot();

    //    Task<IEnumerable<IDirectoryEntry>> Read(IDirectoryEntry entry);

    //    Task<IDirectoryEntry> Create(IDirectoryEntry root, IDirectoryEntryDescriptor descriptor);

    //    Task<bool> Delete(IDirectoryEntry directory);

    //    Task<IDirectoryEntry> Update(IDirectoryEntry directory, IDirectoryEntryDescriptor descriptor);
    //}

    //internal interface IFileManager
    //{

    //}

    //internal interface IFatSystem
    //{
    //    IEnumerable<IFatEntry> ReadRoot();

    //    IEnumerable<IFatEntry> ReadChain();
    //}
}
