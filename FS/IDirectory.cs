using System;

namespace FS
{
    internal interface IDirectory : IBlockHandle, ISupportsFlush, IDisposable
    {
        IDirectory OpenDirectory(string name, OpenMode openMode);

        IFile OpenFile(string name, OpenMode openMode);

        void DeleteFile(string name);

        void DeleteDirectory(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();

        void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides entry);
    }
}