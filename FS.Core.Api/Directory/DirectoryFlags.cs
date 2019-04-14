using System;

namespace FS.Core.Api.Directory
{
    [Flags]
    public enum DirectoryFlags : uint
    {
        None = 0,
        File = 0b0001,
        Directory = 0b0010,
        Deleted = 0b1000
    }
}
