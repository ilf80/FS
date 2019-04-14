using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;

namespace FS.Core.Api.Common
{
    public interface ICommonAccessParameters
    {
        IBlockStorage Storage { get; }

        IAllocationManager AllocationManager { get; }
    }
}
