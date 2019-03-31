namespace FS.Contracts
{
    public static class Constants
    {
        public const uint BlockSize = 512;
    }

    internal class IOVoid
    {
        public static readonly IOVoid Instance = new IOVoid();
    }
}
