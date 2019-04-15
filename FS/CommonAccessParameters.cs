using System;

namespace FS
{
    internal sealed class CommonAccessParameters : ICommonAccessParameters
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