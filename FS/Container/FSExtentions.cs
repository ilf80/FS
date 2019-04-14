using Unity;
using Unity.Extension;
using FS.Api;

namespace FS.Container
{
    public sealed class FSExtentions : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IFileSystem, FileSystem>()
                .RegisterType<IDirectoryEntry, DirectoryEntry>()
                .RegisterType<IFileEntry, FileEntry>();
        }
    }
}
