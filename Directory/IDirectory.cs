using FS.BlockAccess;
using System;

namespace FS.Directory
{
    internal interface IDirectory : IFlushable, IDisposable
    {
        IDirectory OpenDirectory(string name);

        IFile OpenFile(string name);

        //void Delete(string name);

        //void Rename(string name, string newName);

        //bool Exists(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();

        void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides entry);
    }
}
