using FS.Core.Api.Directory;

namespace FS.Core.Directory
{
    internal interface IDeletionFile : IFile
    {
        void Delete();
    }
}
