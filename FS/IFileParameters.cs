namespace FS
{
    internal interface IFileParameters
    {
        int BlockId { get; }

        int ParentDirectoryBlockId { get; }

        int Size { get; }
    }
}