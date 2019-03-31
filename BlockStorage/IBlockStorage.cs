using FS.Contracts;
using System.Threading.Tasks;

namespace FS.BlockStorage
{
    internal interface IBlockStorage
    {
        uint TotalSize { get; }

        ushort BlockSize { get; }

        Task<IOVoid> ReadBlock(int blockIndex, ref byte[] buffer);

        Task<IOVoid> WriteBlock(int blockIndex, byte[] buffer);

        Task<IOVoid> ReadBlock<T>(uint blockIndex, out T target) where T : struct;

        Task<IOVoid> WriteBlock<T>(uint blockIndex, ref T target) where T : struct;
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
