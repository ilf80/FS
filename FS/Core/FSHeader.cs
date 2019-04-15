using System.Runtime.InteropServices;

namespace FS.Core
{
    [StructLayout(LayoutKind.Explicit, Size = 512)]
    // ReSharper disable once InconsistentNaming
    internal struct FSHeader
    {
        [FieldOffset(0)] public int AllocationBlock;

        [FieldOffset(4)] public int FreeBlockCount;

        [FieldOffset(8)] public int RootDirectoryBlock;
    }

    [StructLayout(LayoutKind.Sequential, Size = 512)]
    internal struct DirectoryHeaderRoot
    {
        public DirectoryHeader Header;
    }
}