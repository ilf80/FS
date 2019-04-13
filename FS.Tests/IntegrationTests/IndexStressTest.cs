using FS.Api;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FS.Tests.IntegrationTests
{
    [TestFixture]
    public sealed class IndexStressTest
    {
        private string filePath;
        private Stream stream;

        [SetUp]
        public void SetUp()
        {
            this.filePath = Path.Combine(Path.GetTempPath(), "TestFile.dat");

            var assembly = Assembly.GetExecutingAssembly();
            this.stream = assembly.GetManifestResourceStream("FS.Tests.Program.cs");
        }

        [TearDown]
        public void TearDown()
        {
            this.stream.Close();
            if (System.IO.File.Exists(this.filePath))
            {
                System.IO.File.Delete(this.filePath);
            }
        }

        [Test]
        public void GenerateDirectories()
        {
            var nameList = new List<string>();
            using (var fs = FileSystem.Create(this.filePath))
            {
                var root = fs.GetRootDirectory();


                for (var i = 0; i < 1000; i++)
                {
                    var name = "Dir " + i;
                    root.OpenDirectory(name, OpenMode.OpenOrCreate);
                    nameList.Add(name);
                }
            }

            using (var fs = FileSystem.Open(this.filePath))
            {
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        [TestCase(10, 256)]
        [TestCase(10, 1024)]
        [TestCase(10, 512)]
        [TestCase(5, 2048)]
        public void GenerateBigFile(int mb, int buffSize)
        {
            TestContext.WriteLine($"Generating {mb}Mb file");
            var stopWatch = new Stopwatch();
            using (var fs = FileSystem.Create(this.filePath))
            {
                var root = fs.GetRootDirectory();

                var buffer = new byte[buffSize];
                for(var i = 0; i<buffer.Length; i++)
                {
                    buffer[i] = (byte)i;
                }
                using(var file = root.OpenFile("Test File", OpenMode.Create))
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

            using (var fs = FileSystem.Open(this.filePath))
            {
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
                        };
                    }
                }

            }
        }
    }
}
