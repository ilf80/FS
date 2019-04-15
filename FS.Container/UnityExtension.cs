using FS.Core;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace FS.Container
{
    public class UnityExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IFileSystem, FileSystem>()
                .RegisterType<IDirectoryEntry, DirectoryEntry>()
                .RegisterType<IFileEntry, FileEntry>()
                .RegisterType(typeof(IFactory<>), typeof(Factory<>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IFactory<,>), typeof(Factory<,>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IFactory<,,,>), typeof(Factory<,,,>), null, new TransientLifetimeManager())
                .RegisterType<IAllocationManager, AllocationManager>()
                .RegisterType<IBlockStorage, BlockStorage>()
                .RegisterType(typeof(IBlockStream<>), typeof(BlockStream<>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IIndex<>), typeof(Index<>), null, new TransientLifetimeManager())
                .RegisterType<IIndexBlockProvider, IndexBlockProvider>()
                .RegisterType<IDeletionDirectory, DeletionDirectory>()
                .RegisterType<IDeletionFile, DeletionFile>()
                .RegisterType<IDirectory, Directory>()
                .RegisterType<IUnsafeDirectory, Directory>()
                .RegisterType<IDirectoryCache, DirectoryCache>()
                .RegisterType<IDirectoryEntryInfo, DirectoryEntryInfo>()
                .RegisterType<IFile, File>()
                .RegisterType<IFileSystemProvider, FileSystemProvider>()
                .RegisterType<IUnsafeDirectoryReader, UnsafeDirectoryReader>();
        }
    }
}