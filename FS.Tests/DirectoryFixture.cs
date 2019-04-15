using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class DirectoryFixture
    {
        private Mock<IIndex<DirectoryItem>> index;
        private Mock<IDirectoryCache> directoryCache;
        private DirectoryHeader header;
        private Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>> indexBlockProviderFactory;
        private Mock<IFactory<IIndex<short>, IIndexBlockProvider, ICommonAccessParameters>> indexFactory;
        private Mock<IFactory<IBlockStream<short>, IBlockProvider<short>>> blockStreamFactory;
        private Mock<IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters>> directoryIndexFactory;
        private Mock<IFactory<IDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader>> directoryFactory;
        private Mock<IFactory<IFile, IFileParameters, IDirectoryCache>> fileFactory;
        private Mock<IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>>> directoryBlockStreamFactory;
        private Mock<IFactory<IDeletionFile, IFileParameters, IDirectoryCache>> deletionFileFactory;
        private Mock<IFactory<IDeletionDirectory, int, IDirectoryCache>> deletionDirectoryFactory;
        private Mock<IBlockStream<DirectoryItem>> directoryBlockStream;
        private Mock<IIndexBlockProvider> indexBlockProvider;
        private Mock<IIndex<short>> nameIndex;
        private Mock<IBlockStream<short>> nameBlockStream;
        private Mock<IAllocationManager> allocationManager;
        private Mock<IIndex<DirectoryItem>> directoryIndex;
        private Mock<IDirectory> directory;
        private Mock<IFile> file;
        private Mock<IDeletionFile> deletionFile;
        private Mock<IDeletionDirectory> deletionDirectory;

        [SetUp]
        public void SetUp()
        {
            index = new Mock<IIndex<DirectoryItem>>();
            directoryCache = new Mock<IDirectoryCache>();

            allocationManager = new Mock<IAllocationManager>();
            directoryCache.SetupGet(x => x.AllocationManager).Returns(allocationManager.Object);

            header = new DirectoryHeader();

            indexBlockProvider = new Mock<IIndexBlockProvider>();
            indexBlockProviderFactory = new Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>>();
            indexBlockProviderFactory.Setup(x => x.Create(It.IsAny<int>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(indexBlockProvider.Object);

            nameIndex = new Mock<IIndex<short>>();
            nameIndex.SetupGet(x => x.BlockSize).Returns(10);
            indexFactory = new Mock<IFactory<IIndex<short>, IIndexBlockProvider, ICommonAccessParameters>>();
            indexFactory.Setup(x => x.Create(It.IsAny<IIndexBlockProvider>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(nameIndex.Object);

            nameBlockStream = new Mock<IBlockStream<short>>();
            blockStreamFactory = new Mock<IFactory<IBlockStream<short>, IBlockProvider<short>>>();
            blockStreamFactory.Setup(x => x.Create(It.IsAny<IBlockProvider<short>>())).Returns(nameBlockStream.Object);

            directoryIndex = new Mock<IIndex<DirectoryItem>>();
            directoryIndexFactory = new Mock<IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters>>();
            directoryIndexFactory
                .Setup(x => x.Create(It.IsAny<IIndexBlockProvider>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(directoryIndex.Object);

            directory = new Mock<IDirectory>();
            directoryFactory =
                new Mock<IFactory<IDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader>>();
            directoryFactory
                .Setup(x => x.Create(It.IsAny<IIndex<DirectoryItem>>(), It.IsAny<IDirectoryCache>(),
                    It.IsAny<DirectoryHeader>())).Returns(directory.Object);

            file = new Mock<IFile>();
            fileFactory = new Mock<IFactory<IFile, IFileParameters, IDirectoryCache>>();
            fileFactory.Setup(x => x.Create(It.IsAny<IFileParameters>(), It.IsAny<IDirectoryCache>()))
                .Returns(file.Object);

            directoryBlockStream = new Mock<IBlockStream<DirectoryItem>>();
            directoryBlockStreamFactory = new Mock<IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>>>();
            directoryBlockStreamFactory.Setup(x => x.Create(It.IsAny<IBlockProvider<DirectoryItem>>()))
                .Returns(directoryBlockStream.Object);

            deletionFile = new Mock<IDeletionFile>();
            deletionFileFactory = new Mock<IFactory<IDeletionFile, IFileParameters, IDirectoryCache>>();
            deletionFileFactory.Setup(x => x.Create(It.IsAny<IFileParameters>(), It.IsAny<IDirectoryCache>()))
                .Returns(deletionFile.Object);

            deletionDirectory = new Mock<IDeletionDirectory>();
            deletionDirectoryFactory = new Mock<IFactory<IDeletionDirectory, int, IDirectoryCache>>();
            deletionDirectoryFactory.Setup(x => x.Create(It.IsAny<int>(), It.IsAny<IDirectoryCache>()))
                .Returns(deletionDirectory.Object);

            SetupDirectoryEntries(new[]
            {
                new Entry
                {
                    BlockIndex = 1,
                    Created = new DateTime(2019, 4, 15),
                    Updated = new DateTime(2019, 4, 15),
                    Size = 1,
                    Flags = DirectoryFlags.Directory,
                    Name = "Dir 1"
                },
                new Entry
                {
                    BlockIndex = 2,
                    Created = new DateTime(2019, 4, 14),
                    Updated = new DateTime(2019, 4, 14),
                    Size = 2,
                    Flags = DirectoryFlags.Directory,
                    Name = "Dir 2"
                },
                new Entry
                {
                    BlockIndex = 3,
                    Created = new DateTime(2019, 4, 13),
                    Updated = new DateTime(2019, 4, 11),
                    Size = 2,
                    Flags = DirectoryFlags.File,
                    Name = "File 1"
                },
                new Entry
                {
                    BlockIndex = 3,
                    Created = new DateTime(2019, 4, 13),
                    Updated = new DateTime(2019, 4, 11),
                    Size = 2,
                    Flags = DirectoryFlags.File | DirectoryFlags.Deleted,
                    Name = "Deleted File 1"
                }
            });
        }

        [Test]
        public void ShouldReturnEntries()
        {
            // Given
            var instance = CreateInstance();

            // When
            var result = instance.GetDirectoryEntries();

            // Then
            Assert.AreEqual(3, result.Length);

            Assert.AreEqual("Dir 1", result[0].Name);
            Assert.AreEqual(true, result[0].IsDirectory);
            Assert.AreEqual(1, result[0].BlockId);

            Assert.AreEqual("Dir 2", result[1].Name);
            Assert.AreEqual(true, result[1].IsDirectory);
            Assert.AreEqual(2, result[1].BlockId);

            Assert.AreEqual("File 1", result[2].Name);
            Assert.AreEqual(false, result[2].IsDirectory);
            Assert.AreEqual(3, result[2].BlockId);
        }

        [Test]
        public void ShouldUpdateEntry()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.UpdateEntry(2,
                Mock.Of<IDirectoryEntryInfoOverrides>(y =>
                    y.Size == 10 && y.Flags == DirectoryFlags.File && y.Updated == new DateTime(2019, 4, 20)));

            // Then
            directoryBlockStream.Verify(x => x.Write(2,
                It.Is<DirectoryItem[]>(y =>
                    y.Length == 1 && y[0].Entry.Size == 10 && y[0].Entry.Flags == DirectoryFlags.File &&
                    y[0].Entry.Updated == new DateTime(2019, 4, 20).Ticks)));
        }

        [Test]
        public void ShouldOpenExistingDirectory()
        {
            // Given
            var instance = CreateInstance();

            var dir = Mock.Of<IDirectory>();
            directoryCache.Setup(x => x.ReadDirectory(It.IsAny<int>())).Returns(dir);

            // When
            var result = instance.OpenDirectory("Dir 1", OpenMode.OpenExisting);

            //Then
            Assert.AreEqual(dir, result);
        }

        [Test]
        public void ShouldOpenExistingFile()
        {
            // Given
            var instance = CreateInstance();
            directoryCache.Setup(x => x.ReadFile(It.IsAny<int>(), It.IsAny<Func<IFile>>()))
                .Returns((int blockId, Func<IFile> factory) => factory());

            // When
            var result = instance.OpenFile("File 1", OpenMode.OpenExisting);

            //Then
            Assert.AreEqual(file.Object, result);
        }

        [Test]
        public void ShouldNotCreateExistingDirectory()
        {
            // Given
            var instance = CreateInstance();

            // When
            //Then
            Assert.Throws<InvalidOperationException>(delegate { instance.OpenDirectory("Dir 1", OpenMode.Create); });
        }

        [Test]
        public void ShouldNotCreateExistingFile()
        {
            // Given
            var instance = CreateInstance();

            // When
            //Then
            Assert.Throws<InvalidOperationException>(delegate { instance.OpenFile("File 1", OpenMode.Create); });
        }

        [Test]
        public void ShouldNotOpenNonExistingDirectory()
        {
            // Given
            var instance = CreateInstance();

            // When
            //Then
            Assert.Throws<InvalidOperationException>(delegate { instance.OpenDirectory("Dir 111", OpenMode.OpenExisting); });
        }

        [TestCase(OpenMode.Create)]
        [TestCase(OpenMode.OpenOrCreate)]
        public void ShouldCreateDirectory(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            allocationManager.Setup(x => x.Allocate(2)).Returns(new[] {1, 2});
            directoryIndex.SetupGet(x => x.BlockId).Returns(2);
            directoryCache.Setup(x => x.RegisterDirectory(It.IsAny<IDirectory>())).Returns((IDirectory dir) => dir);

            indexBlockProviderFactory.Invocations.Clear();

            // When
            var result = instance.OpenDirectory("Test", mode);

            // Then
            Assert.AreEqual(directory.Object, result);
            indexBlockProviderFactory.Verify(x => x.Create(2, It.IsAny<ICommonAccessParameters>()), Times.Once);
            directory.Verify(x => x.Flush(), Times.Once);
            directoryIndex.Verify(x => x.SetSizeInBlocks(1), Times.Once);
            directoryIndex.Verify(x => x.Flush(), Times.Once);
            directoryCache.Verify(x => x.RegisterDirectory(directory.Object), Times.Once);
            directoryBlockStream.Verify(x => x.Write(5,
                It.Is<DirectoryItem[]>(y =>
                    y.Length == 1 && y[0].Entry.BlockIndex == 2 && y[0].Entry.Flags == DirectoryFlags.Directory)));
            index.Verify(x => x.Flush(), Times.Once);
        }

        [TestCase(OpenMode.Create)]
        [TestCase(OpenMode.OpenOrCreate)]
        public void ShouldCreateFile(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            allocationManager.Setup(x => x.Allocate(1)).Returns(new[] { 2 });
            directoryCache.Setup(x => x.RegisterFile(It.IsAny<IFile>())).Returns((IFile tmpFile) => tmpFile);

            indexBlockProviderFactory.Invocations.Clear();

            // When
            var result = instance.OpenFile("Test", mode);

            // Then
            Assert.AreEqual(file.Object, result);
            fileFactory.Verify(
                x => x.Create(
                    It.Is<IFileParameters>(y => y.Size == 0 && y.BlockId == 2 && y.ParentDirectoryBlockId == 0),
                    It.IsAny<IDirectoryCache>()), Times.Once);
            file.Verify(x => x.Flush(), Times.Once);
            directoryCache.Verify(x => x.RegisterFile(file.Object), Times.Once);
            directoryBlockStream.Verify(x => x.Write(5,
                It.Is<DirectoryItem[]>(y =>
                    y.Length == 1 && y[0].Entry.BlockIndex == 2 && y[0].Entry.Flags == DirectoryFlags.File)));
            index.Verify(x => x.Flush(), Times.Once);
        }

        [Test]
        public void ShouldDeleteFile()
        {
            // Given
            var instance = CreateInstance();
            directoryCache.Setup(x => x.ReadFile(It.IsAny<int>(), It.IsAny<Func<IFile>>()))
                .Returns((int blockId, Func<IFile> factory) => factory());

            // When
            instance.DeleteFile("File 1");

            // Then
            deletionFileFactory.Verify(
                x => x.Create(
                    It.Is<IFileParameters>(y => y.BlockId == 3 && y.ParentDirectoryBlockId == 0),
                    It.IsAny<IDirectoryCache>()), Times.Once);
        }

        [Test]
        public void ShouldDeleteDirectory()
        {
            // Given
            var instance = CreateInstance();
            directoryCache.Setup(x => x.RegisterDirectory(It.IsAny<IDirectory>()))
                .Returns((IDirectory dir) => dir);

            // When
            instance.DeleteDirectory("Dir 1");

            // Then
            deletionDirectoryFactory.Verify(x => x.Create(1, It.IsAny<IDirectoryCache>()), Times.Once);
            deletionDirectory.Verify(x => x.Delete(), Times.Once);
        }

        private void SetupDirectoryEntries(Entry[] entries)
        {
            var bufferLength = entries.Select(x => x.Name.Length + 1).Sum() * 2;

            var namesBuffer = new short[bufferLength];
            var offsets = SetupNames(entries, namesBuffer);

            nameBlockStream.Setup(x => x.Read(0, It.IsAny<short[]>())).Callback((int position, short[] buffer) =>
                Array.Copy(namesBuffer, buffer, namesBuffer.Length));

            header.LastNameOffset = bufferLength;
            header.ItemsCount = entries.Length;
            header.FirstEmptyItemOffset = 5;

            var directoryItems = entries.Zip(offsets, (e, o) => new DirectoryItem
            {
                Entry = new DirectoryEntryStruct
                {
                    BlockIndex = e.BlockIndex,
                    Created = e.Created.Ticks,
                    Updated = e.Updated.Ticks,
                    Flags = e.Flags,
                    Size = e.Size,
                    NameOffset = o
                }
            }).ToArray();
            directoryBlockStream.Setup(x => x.Read(1, It.IsAny<DirectoryItem[]>())).Callback(
                (int i, DirectoryItem[] buffer) => Array.Copy(directoryItems, buffer, directoryItems.Length));
        }

        private static IEnumerable<int> SetupNames(IEnumerable<Entry> entries, short[] namesBuffer)
        {
            var offsets = new List<int>();

            var bufferOffset = 0;
            foreach (var entry in entries)
            {
                offsets.Add(bufferOffset);
                var temp = new[] {(short) entry.Name.Length}.Concat(entry.Name.Select(x => (short) x)).ToArray();
                Array.Copy(temp, 0, namesBuffer, bufferOffset, temp.Length);
                bufferOffset += temp.Length;
            }

            return offsets.ToArray();
        }

        private class Entry
        {
            public DirectoryFlags Flags { get; set; }

            public int Size { get; set; }

            public DateTime Created { get; set; }

            public DateTime Updated { get; set; }

            public int BlockIndex { get; set; }

            public string Name { get; set; }
        }

        private Directory CreateInstance()
        {
            return new Directory(index.Object, directoryCache.Object, header, indexBlockProviderFactory.Object,
                indexFactory.Object, blockStreamFactory.Object, directoryIndexFactory.Object, directoryFactory.Object,
                fileFactory.Object, directoryBlockStreamFactory.Object, deletionFileFactory.Object,
                deletionDirectoryFactory.Object);
        }
    }
}
