using FS.Core.Allocation;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Api.Directory;
using FS.Core.Api.FileSystem;
using FS.Core.BlockAccess;
using FS.Core.BlockAccess.Indexes;
using FS.Core.Directory;
using FS.Core.FileSystem;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace FS.Core.Container
{
    public class CoreRegistration : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IAllocationManager, AllocationManager>()
                .RegisterType<IBlockStorage, BlockStorage>()
                .RegisterType(typeof(IBlockStream<>), typeof(BlockStream<>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IIndex<>), typeof(Index<>), null, new TransientLifetimeManager())
                .RegisterType<IIndexBlockProvider, IndexBlockProvider>()
                .RegisterType<IDeletionDirectory, DeletionDirectory>()
                .RegisterType<IDeletionFile, DeletionFile>()
                .RegisterType<IDirectory, Directory.Directory>()
                .RegisterType<IUnsafeDirectory, Directory.Directory>()
                .RegisterType<IDirectoryCache, DirectoryCache>()
                .RegisterType<IDirectoryEntryInfo, DirectoryEntryInfo>()
                .RegisterType<IFile, File>()
                .RegisterType<IFileSystemProvider, FileSystemProvider>()
                .RegisterType<IUnsafeDirectoryReader, UnsafeDirectoryReader>();
        }
    }
}
