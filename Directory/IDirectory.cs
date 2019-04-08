using FS.BlockAccess;

namespace FS.Directory
{
    internal interface IDirectory : IFlushable
    {
        IDirectory OpenDirectory(string name);

        IFile OpenFile(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();
    }
}
