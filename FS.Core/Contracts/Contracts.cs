namespace FS.Core.Contracts
{
    internal static class Constants
    {
        public const int BlockSize = 512;

        public const int EmptyBlockIndex = 0;

        public const int IndexPageSize = BlockSize / IndexEntrySize;

        public const int MaxItemsInIndexPage = IndexPageSize - 1;

        public const int IndexEntrySize = 4;
    }
}
