﻿using FS.Allocattion;
using FS.Contracts;
using FS.Directory;
using FS.Contracts.Indexes;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using bs = FS.Contracts.BlockStorage;
using FS.Api;
using System.Linq;
using System.Threading;

namespace FS
{
    [StructLayout(LayoutKind.Sequential, Size = 512)]
    struct DirectoryHeaderRoot
    {
        public DirectoryHeader Header;
    }

    class Program
    {
        static void PrindDirectory(IDirectoryEntry root, string path)
        {
            if (path == "ROOT/Dir 9/Dir 9.1") return;

            var entries = root.GetEntries();

            foreach (var entry in entries.Where(x => x.IsDirectory).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{path}/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
                using (var d = root.OpenDirectory(entry.Name, OpenMode.OpenExisting))
                {
                    PrindDirectory(d, path + "/" + entry.Name);
                }
            }

            foreach (var entry in entries.Where(x => !x.IsDirectory).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{path}/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
            }

            
        }
        static async Task Main(string[] args)
        {
            using (var fs = FileSystem.Open("TestFile.dat"))
            {
                var root = fs.GetRootDirectory();

                PrindDirectory(root, "ROOT");

                using (var d = root.OpenDirectory("Dir 8", OpenMode.OpenExisting))
                {
                    var t = new Task(() =>
                    {
                        for (int i = 100; i < 200; i++)
                        {
                            d.OpenDirectory("Dir 8." + i + "." + Thread.CurrentThread.ManagedThreadId, OpenMode.OpenOrCreate);
                        }
                    });

                    var t2 = new Task(() =>
                    {
                        for (int i = 100; i < 200; i++)
                        {
                            d.OpenDirectory("Dir 8." + i + "." + Thread.CurrentThread.ManagedThreadId, OpenMode.OpenOrCreate);
                        }
                    });

                    //t.Start(); t2.Start();

                    //Task.WaitAll(t, t2);

                    //d.OpenDirectory("Dir 9.1", OpenMode.OpenOrCreate);
                    //d.OpenDirectory("Dir 9.2", OpenMode.OpenOrCreate);
                }
            }

                return;
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
                //    NameBlockIndex = 4,
                //    ParentDirectoryBlockIndex = 2
                //};
                //blockStorage.WriteBlock(3, new[] { new DirectoryHeaderRoot { Header = fsRoot } });
                //blockStorage.Dispose();
                //return;

                blockStorage.Open();

                var header = new FSHeader[1];
                blockStorage.ReadBlock(0, header);


                Func<IAllocationManager, IIndex<int>> allocationIndexFactory = (IAllocationManager m) => {
                    IIndexBlockProvier allocationIndexProvider = new IndexBlockProvier(header[0].AllocationBlock, m, blockStorage);
                    return new Index<int>(allocationIndexProvider, new BlockStream<int>(allocationIndexProvider), m, blockStorage);
                };
                var allocationManager = new AllocationManager(allocationIndexFactory, blockStorage, header[0].FreeBlockCount);

                //var indexBlockChainProvider = new IndexBlockChainProvier(header[0].RootDirectoryBlock, allocationManager, blockStorage);
                //var index = new Index<DirectoryItem>(indexBlockChainProvider, new BlockChain<int>(indexBlockChainProvider), blockStorage, allocationManager);

                //index.SetSizeInBlocks(1);
                //index.Flush();

//                var rootDir = DirectoryManager.ReadDirectory(header[0].RootDirectoryBlock, blockStorage, allocationManager);

                //rootDir.OpenDirectory("Test");

                //for (var i = 0; i < 10; i++)
                //{
                //    rootDir.OpenDirectory("Dir " + i);
                //}

//                var file = rootDir.OpenFile("Test File 3");
                //file.SetSize(128);
                //file.Write(0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                //file.Flush();

                var buffer = new byte[10];
//                file.Read(0, buffer);

                for(var i = 0; i < buffer.Length; i++)
                {
                    Console.Write(buffer[i].ToString("X2"));
                    Console.Write(' ');
                }
                Console.WriteLine();

//                var dir = rootDir.OpenDirectory("Dir 0");
                //dir.OpenDirectory("Dir 0.1");
                //dir.OpenDirectory("Dir 0.2");

                //foreach (var entry in dir.GetDirectoryEntries())
                //{
                //    Console.WriteLine($"Enrty: Name Dir 0/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
                //}
                //dir.Flush();

                //file.SetSize(200);
                //file.Write(0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                //file.Flush();

                //rootDir.CreateDirectory(new string('0', 10123));

                //rootDir.CreateDirectory(new string('1', 250000000));

                ////foreach (var entry in rootDir.GetDirectoryEntries())
                ////{
                ////    Console.WriteLine($"Enrty: Name {entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
                ////}


                //rootDir.CreateDirectory("Test1");


                //blockChain.Read(0, buffer);

                //index.Flush();

                //rootDir.Flush();
                allocationManager.Flush();

                header[0].FreeBlockCount = allocationManager.ReleasedBlockCount;
                blockStorage.WriteBlock(0, header);
            }
        }
    }
}
