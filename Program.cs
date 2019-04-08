using FS.Allocattion;
using FS.BlockAccess;
using FS.Directory;
using FS.BlockAccess.Indexes;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using bs = FS.BlockAccess.BlockStorage;
namespace FS
{
    [StructLayout(LayoutKind.Sequential, Size = 512)]
    struct DirectoryHeaderRoot
    {
        public DirectoryHeader Header;
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var taskFactory = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.ExecuteSynchronously);
            using (var blockStorage = new bs("TestFile.dat"))
            {
                //blockStorage.Open();
                //var b = new byte[512];
                //blockStorage.WriteBlock(0, b);
                //blockStorage.WriteBlock(1, b);
                //blockStorage.WriteBlock(2, b);
                //blockStorage.WriteBlock(3, b);
                //blockStorage.WriteBlock(4, b);

                //var fsHeader = new FSHeader
                //{
                //    AllocationBlock = 1,
                //    RootDirectoryBlock = 2,
                //    FreeBlockCount = 0
                //};
                //blockStorage.WriteBlock(0, new[] { fsHeader });

                //b[0] = 3;
                //blockStorage.WriteBlock(2, b);

                //var fsRoot = new DirectoryHeader
                //{
                //    FirstEmptyItemOffset = 1,
                //    ItemsCount = 0,
                //    LastNameOffset = 0,
                //    NameBlockIndex = 4
                //};
                //blockStorage.WriteBlock(3, new[] { new DirectoryHeaderRoot { Header = fsRoot } });
                //blockStorage.Dispose();
                //return;

                blockStorage.Open();

                var header = new FSHeader[1];
                blockStorage.ReadBlock(0, header);


                //var buffer = new int[512 + 512 * 2];
                //for(var i = 0; i<buffer.Length; i++)
                //{
                //    buffer[i] = i;
                //}
                //await blockStorage.WriteBlock(0, buffer);
                //buffer[0] = 2;
                //await blockStorage.WriteBlock(1, buffer);
                //return;

                Func<IAllocationManager, IIndex<int>> allocationIndexFactory = (IAllocationManager m) => {
                    IIndexBlockProvier allocationIndexProvider = new IndexBlockProvier(header[0].AllocationBlock, m, blockStorage);
                    return new Index<int>(allocationIndexProvider, new BlockStream<int>(allocationIndexProvider), blockStorage, m);
                };
                var allocationManager = new AllocationManager(allocationIndexFactory, blockStorage, header[0].FreeBlockCount);

                //var indexBlockChainProvider = new IndexBlockChainProvier(header[0].RootDirectoryBlock, allocationManager, blockStorage);
                //var index = new Index<DirectoryItem>(indexBlockChainProvider, new BlockChain<int>(indexBlockChainProvider), blockStorage, allocationManager);

                //index.SetSizeInBlocks(1);
                //index.Flush();

                var rootDir = DirectoryManager.ReadDirectory(header[0].RootDirectoryBlock, blockStorage, allocationManager);

                //rootDir.CreateDirectory("Test");

                //for (var i = 0; i < 10; i++)
                //{
                //    rootDir.CreateDirectory("Dir " + i);
                //}

                var file = rootDir.OpenFile("Test File 3");
                //file.SetSize(512);
                //file.Write(0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                //file.Flush();

                var buffer = new byte[10];
                file.Read(0, buffer);

                for(var i = 0; i < buffer.Length; i++)
                {
                    Console.Write(buffer[i].ToString("X2"));
                    Console.Write(' ');
                }
                //file.SetSize(512);
                //file.Write(0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                //file.Flush();

                //rootDir.CreateDirectory(new string('0', 10123));

                //rootDir.CreateDirectory(new string('1', 250000000));

                foreach (var entry in rootDir.GetDirectoryEntries())
                {
                    Console.WriteLine($"Enrty: Name {entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}");
                }


                //rootDir.CreateDirectory("Test1");


                //blockChain.Read(0, buffer);

                //index.Flush();

                rootDir.Flush();
                allocationManager.Flush();

                header[0].FreeBlockCount = allocationManager.ReleasedBlockCount;
                blockStorage.WriteBlock(0, header);
            }
        }
    }
}
