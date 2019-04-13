using FS.Api;
using FS.BlockAccess;
using System;

namespace FS.Directory
{
    internal interface IDirectory : IBlockHandle, IFlushable, IDisposable
    {
        IDirectory OpenDirectory(string name, OpenMode openMode);

        IFile OpenFile(string name, OpenMode openMode);

        void DeleteFile(string name);

        void DeleteDirectory(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();

        void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides entry);
    }
}
