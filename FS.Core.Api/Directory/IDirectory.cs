using System;
using FS.Api;
using FS.Core.Api.Common;

namespace FS.Core.Api.Directory
{
    public interface IDirectory : IBlockHandle, ISupportsFlush, IDisposable
    {
        IDirectory OpenDirectory(string name, OpenMode openMode);

        IFile OpenFile(string name, OpenMode openMode);

        void DeleteFile(string name);

        void DeleteDirectory(string name);

        IDirectoryEntryInfo[] GetDirectoryEntries();

        void UpdateEntry(int blockId, IDirectoryEntryInfoOverrides entry);
    }
}
