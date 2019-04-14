using FS.BlockAccess;
using System;
using FS.Allocation;

namespace FS.Directory
{
    internal interface IDirectoryCache
    {
        IBlockStorage Storage { get; }

        IAllocationManager AllocationManager { get; }

        IDirectory ReadDirectory(int blockId);

        IDirectory RegisterDirectory(IDirectory directory);

        void UnRegisterDirectory(int blockId);

        IFile ReadFile(int blockId, Func<IFile> readFile);

        IFile RegisterFile(IFile file);

        void UnRegisterFile(int blockId);
    }
}
