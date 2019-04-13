﻿namespace FS.BlockAccess.Indexes
{
    internal interface IIndexBlockProvier : IBlockProvider<int>, IFlushable
    {
        int BlockId { get; }

        int UsedEntryCount { get; }
    }
}
