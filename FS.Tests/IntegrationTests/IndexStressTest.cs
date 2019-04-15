using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FS.Container;
using NUnit.Framework;
using Unity;

namespace FS.Tests.IntegrationTests
{
    [TestFixture]
    public sealed class IndexStressTest
    {
        [SetUp]
        public void SetUp()
        {
            container = new UnityContainer()
                .AddExtension(new UnityExtension())
                .AddExtension(new Diagnostic());

            filePath = Path.Combine(Path.GetTempPath(), "TestFile.dat");

            var assembly = Assembly.GetExecutingAssembly();
            stream = assembly.GetManifestResourceStream("FS.Tests.Program.cs");
        }

        [TearDown]
        public void TearDown()
        {
            stream.Close();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string filePath;
        private Stream stream;
        private IUnityContainer container;

        [Test]
        [TestCase(10, 256)]
        [TestCase(10, 1024)]
        [TestCase(10, 512)]
        [TestCase(5, 2048)]
        public void GenerateBigFile(int mb, int buffSize)
        {
            TestContext.WriteLine($"Generating {mb}Mb file");
            var stopWatch = new Stopwatch();
            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.Create);
                var root = fs.GetRootDirectory();

                var buffer = new byte[buffSize];
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)i;
                }

                using (var file = root.OpenFile("Test File", OpenMode.Create))
                {
                    stopWatch.Start();
                    file.SetSize(mb * 1024 * 1024);
                    TestContext.WriteLine($"Allocation done in {stopWatch.ElapsedMilliseconds}ms");

                    for (var i = 0; i < mb * 1024 * 1024 / buffer.Length; i++)
                    {
                        file.Write(i * buffer.Length, buffer);
                    }

                    TestContext.WriteLine($"Write done in {stopWatch.ElapsedMilliseconds}ms");
                }

                stopWatch.Stop();
            }

            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.OpenExisting);
                var root = fs.GetRootDirectory();

                var buffer = new byte[buffSize];
                using (var file = root.OpenFile("Test File", OpenMode.OpenExisting))
                {
                    for (var i = 0; i < mb * 1024 * 1024 / buffer.Length; i++)
                    {
                        file.Read(i * buffer.Length, buffer);
                        for (var j = 0; j < buffer.Length; j++)
                        {
                            if ((byte)j != buffer[j])
                            {
                                Assert.Fail($"Incorrect data read! Should be {(byte)j} but was {buffer[j]}");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void GenerateDirectories()
        {
            var nameList = new List<string>();
            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.Create);
                var root = fs.GetRootDirectory();


                for (var i = 0; i < 1000; i++)
                {
                    var name = "Dir " + i;
                    root.OpenDirectory(name, OpenMode.OpenOrCreate);
                    nameList.Add(name);
                }
            }

            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.OpenExisting);
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }
    }
}