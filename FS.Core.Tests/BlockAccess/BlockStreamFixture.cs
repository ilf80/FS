using System;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using FakeItEasy;
using FS.Core.Api.BlockAccess;
using FS.Core.BlockAccess;
using NUnit.Framework;

namespace FS.Core.Tests.BlockAccess
{
    [TestFixture]
    internal sealed class BlockStreamFixture
    {
        private IBlockProvider<byte> provider;
        private byte[] readBuffer;
        private byte[] readBuffer2;

        [SetUp]
        public void SetUp()
        {
            provider = A.Fake<IBlockProvider<byte>>();

            A.CallTo(() => provider.BlockSize).Returns(17);
            A.CallTo(() => provider.EntrySize).Returns(1);
            A.CallTo(() => provider.SizeInBlocks).Returns(2);
            A.CallTo(() => provider.Read(0, A<byte[]>._)).Invokes((int position, byte[] buffer) =>
            {
                Array.Copy(readBuffer, buffer, readBuffer.Length);
            });
            A.CallTo(() => provider.Read(1, A<byte[]>._)).Invokes((int position, byte[] buffer) =>
            {
                Array.Copy(readBuffer2, buffer, readBuffer2.Length);
            });

            readBuffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
            readBuffer2 = new byte[] { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37 };
        }

        [Test]
        [TestCase(0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 })]
        [TestCase(0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [TestCase(1, new byte[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 })]
        [TestCase(10, new byte[] { 11, 12, 13, 14, 15, 16, 17 })]
        [TestCase(12, new byte[] { 13, 14 })]
        [TestCase(16, new byte[] { 17 })]
        [TestCase(16, new byte[] { 17, 21 })]
        [TestCase(17, new byte[] { 21 })]
        [TestCase(10, new byte[] { 11, 12, 13, 14, 15, 16, 17, 21, 22, 23, 24, 25, 26 })]
        [TestCase(20, new byte[] { 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37 })]
        public void ShouldRead(int position, byte[] expected)
        {
            // Given
            var instance = CreateInstance();
            var result = new byte[expected.Length];

            // When
            instance.Read(position, result);

            // Then
            CollectionAssert.AreEqual(expected, result);
        }

        //[Test]
        //[TestCase(0, new byte[] { 40, 41, 42, 43 }, new byte[] { 40, 41, 42, 43, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 })]
        //[TestCase(5, new byte[] { 40, 41, 42, 43 }, new byte[] { 1, 2, 3, 4, 5, 40, 41, 42, 43, 10, 11, 12, 13, 14, 15, 16, 17 })]
        //public void ShouldWriteFirstBlock(int position, byte[] toWrite, byte[] expected)
        //{
        //    // Given
        //    var instance = CreateInstance();

        //    // When
        //    instance.Write(position, toWrite);

        //    // Then
        //    provider.Verify(x => x.Write(0, It.Is<byte[]>(result => CollectionsAreEqual(expected, result))));
        //}

        //[Test]
        //[TestCase(20, new byte[] { 40, 41, 42, 43 }, new byte[] { 21, 22, 23, 40, 41, 42, 43, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37 })]
        //[TestCase(25, new byte[] { 40, 41, 42, 43 }, new byte[] { 21, 22, 23, 24, 25, 26, 27, 28, 40, 41, 42, 43, 33, 34, 35, 36, 37 })]
        //public void ShouldWriteSecondBlock(int position, byte[] toWrite, byte[] expected)
        //{
        //    // Given
        //    var instance = CreateInstance();

        //    // When
        //    instance.Write(position, toWrite);

        //    // Then
        //    provider.Verify(x => x.Write(1, It.Is<byte[]>(result => CollectionsAreEqual(expected, result))));
        //}

        private BlockStream<byte> CreateInstance()
        {
            return new BlockStream<byte>(provider);
        }

        private static bool CollectionsAreEqual(byte[] a, byte[] b)
        {
            CollectionAssert.AreEqual(a, b);
            return true;
        }
    }
}
