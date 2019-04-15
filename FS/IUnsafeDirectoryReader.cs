namespace FS
{
    internal interface IUnsafeDirectoryReader
    {
        IUnsafeDirectory Read(int blockId);
    }
}