using FS.Allocattion;
using FS.BlockStorage;
using FS.Contracts;
using FS.Indexes;
using System;
using System.Threading.Tasks;
using bs = FS.BlockStorage.BlockStorage2;
namespace FS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var taskFactory = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.ExecuteSynchronously);
            using (var blockStorage = new bs("TestFile.dat"))
            {
                blockStorage.Open();

                var buffer = new int[512 + 512];
                for(var i = 0; i<buffer.Length; i++)
                {
                    buffer[i] = i;
                }
                //await blockStorage.WriteBlock(0, buffer);
                //buffer[0] = 2;
                //await blockStorage.WriteBlock(1, buffer);
                //return;

                var allocationManager = new AllocationManager2();

                var indexBlockChainProvider = new IndexBlockChainProvier(1, allocationManager, blockStorage);

                Console.WriteLine($"Index enty count : {indexBlockChainProvider.UsedEntryCount}");

                var index = new Index<int>(indexBlockChainProvider, new BlockChain<int>(indexBlockChainProvider), blockStorage, allocationManager);
                index.SetSizeInBlocks(buffer.Length * 4 / 512);

                var blockChain = new BlockChain<int>(index);
                blockChain.Write(0, buffer);

                //blockChain.Read(0, buffer);

                index.Flush();
            }
        }
    }
}
