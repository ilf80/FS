using FS.Contracts;
using System.Threading.Tasks;

namespace FS.BlockStorage
{
    internal interface IBlockStorage
    {
        uint TotalSize { get; }

        ushort BlockSize { get; }

        Task ReadBlock(uint blockIndex, byte[] buffer);

        Task WriteBlock(uint blockIndex, byte[] buffer);

        Task ReadBlock<T>(uint blockIndex, out T target) where T : struct;

        Task WriteBlock<T>(uint blockIndex, ref T target) where T : struct;
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
