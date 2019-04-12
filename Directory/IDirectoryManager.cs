using FS.Allocattion;
using FS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS.Directory
{
    internal interface IDirectoryManager
    {
        IBlockStorage Storage { get; }

        IAllocationManager AllocationManager { get; }

        IDirectory ReadDirectory(int blockId);

        IDirectory RegisterDirectory(IDirectory directory);

        void UnRegisterDirectory(int blockId);

        IFile ReadFile(int blockId, Func<IFile> readFile);

        IFile RegisterFile(IFile file);
    }
}
