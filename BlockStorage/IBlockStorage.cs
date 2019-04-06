using System;
using System.Threading.Tasks;

namespace FS.BlockStorage
{
    internal interface IBlockStorage: IDisposable
    {
        uint TotalSize { get; }

        int BlockSize { get; }

        Task ReadBlock(uint blockIndex, byte[] buffer);

        Task WriteBlock(uint blockIndex, byte[] buffer);

        //Task ReadBlock<T>(uint blockIndex, out T target) where T : struct;

        Task<T> ReadBlock<T>(uint blockIndex) where T : struct;

        Task WriteBlock<T>(uint blockIndex, ref T target) where T : struct;
    }

    internal interface IBlockStorage2 : IDisposable
    {
        long TotalSize { get; }

        int BlockSize { get; }

        void ReadBlock(int blockIndex, byte[] buffer);

        void WriteBlock(int blockIndex, byte[] buffer);

        void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct;

        void WriteBlock<T>(int blockIndex, T[] buffer) where T : struct;

        int[] Extend(int blockCount);
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
