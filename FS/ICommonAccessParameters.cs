namespace FS
{
    internal interface ICommonAccessParameters
    {
        IBlockStorage Storage { get; }

        IAllocationManager AllocationManager { get; }
    }
}