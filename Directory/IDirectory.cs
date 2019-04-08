using FS.Contracts;

namespace FS.Directory
{
    internal interface IDirectory : IFlushable
    {
        IDirectory CreateDirectory(string name);

        IFile OpenFile(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();
    }
}
