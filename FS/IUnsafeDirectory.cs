namespace FS
{
    internal interface IUnsafeDirectory : IDirectory
    {
        void UnsafeDeleteDirectory();
    }
}