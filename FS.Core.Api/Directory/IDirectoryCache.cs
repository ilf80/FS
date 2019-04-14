using System;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;

namespace FS.Core.Api.Directory
{
    public interface IDirectoryCache : IDisposable
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
