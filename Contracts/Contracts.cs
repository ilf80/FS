namespace FS.Contracts
{
    internal static class Constants
    {
        public const int BlockSize = 512;

        public const uint EmptyBlockIndex = 0;

        public const int IndexPageSize = 128;

        public const int MaxItemsInIndexPage = IndexPageSize - 1;
    }

    internal class IOVoid
    {
        public static readonly IOVoid Instance = new IOVoid();
    }
}
