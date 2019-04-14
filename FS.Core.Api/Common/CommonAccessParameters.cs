using System;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;

namespace FS.Core.Api.Common
{
    public sealed class CommonAccessParameters : ICommonAccessParameters
    {
        public CommonAccessParameters(IBlockStorage storage, IAllocationManager allocationManager)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            AllocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
        }

        public IBlockStorage Storage { get; }
        public IAllocationManager AllocationManager { get; }
    }
}