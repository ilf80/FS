using FS.Core.Api.Directory;

namespace FS.Core.Directory
{
    internal interface IUnsafeDirectoryReader
    {
        IUnsafeDirectory Read(int blockId);
    }
}
