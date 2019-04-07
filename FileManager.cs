using FS.Directory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FS
{
    [StructLayout(LayoutKind.Explicit, Size = 512)]
    internal struct FSHeader
    {
        [FieldOffset(0)]
        public int AllocationBlock;

        [FieldOffset(4)]
        public int FreeBlockCount;

        [FieldOffset(8)]
        public int RootDirectoryBlock;

        [FieldOffset(12)]
        public DirectoryHeader RootDirectoryHeader;
    }

    class FileManager
    {
    }
}
