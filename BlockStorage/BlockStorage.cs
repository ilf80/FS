using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FS.Contracts;

namespace FS.BlockStorage
{
    internal sealed class BlockDevice : IBlockStorage
    {
        public uint TotalSize => throw new NotImplementedException();

        public ushort BlockSize => throw new NotImplementedException();

        public Task<IOVoid> ReadBlock(int blockIndex, ref byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public Task<IOVoid> ReadBlock<T>(uint blockIndex, out T target) where T : struct
        {
            throw new NotImplementedException();
        }

        public Task<IOVoid> WriteBlock(int blockIndex, byte[] buffer)
        {
            throw new NotImplementedException();
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

        public Task<IOVoid> WriteBlock<T>(uint blockIndex, ref T target) where T : struct
        {
            throw new NotImplementedException();
        }
    }
}
