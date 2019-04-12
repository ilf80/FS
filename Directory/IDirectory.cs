using FS.Api;
using FS.Contracts;
using System;

namespace FS.Directory
{
    internal interface IDirectory : IBlockHandle, IFlushable, IDisposable
    {
        IDirectory OpenDirectory(string name, OpenMode openMode);

        IFile OpenFile(string name, OpenMode openMode);

        //void Delete(string name);

        //void Rename(string name, string newName);

        //bool Exists(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();

        void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides entry);
    }
}
