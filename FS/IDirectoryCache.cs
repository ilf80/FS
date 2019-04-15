using System;

namespace FS
{
    internal interface IDirectoryCache : ICommonAccessParameters, IDisposable
    {
        IDirectory ReadDirectory(int blockId);

        IDirectory RegisterDirectory(IDirectory directory);

        void UnRegisterDirectory(int blockId);

        IFile ReadFile(int blockId, Func<IFile> readFile);

        IFile RegisterFile(IFile file);

        void UnRegisterFile(int blockId);
    }
}