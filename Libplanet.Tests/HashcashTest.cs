using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests
{
    public class HashcashTest
    {
        [Theory]
        [ClassData(typeof(HashcashTestData))]
        public void AnswerHasLeadingZeroBits(byte[] challenge, uint bits)
        {
            byte[] Stamp(Nonce nonce) => challenge.Concat(nonce.ToByteArray()).ToArray();
            var answer = Hashcash.Answer(Stamp, bits);
            var digest = Hashcash.Hash(Stamp(answer));
            Assert.True(digest.HasLeadingZeroBits(bits));
        }

        [Fact]
        public void TestBytesWithLeadingZeroBits()
        {
            Assert.True(HasLeadingZeros(new byte[1] { 0x80 }, 0));
            Assert.False(HasLeadingZeros(new byte[1] { 0x80 }, 1));
            for (uint bits = 0; bits < 9; bits++)
            {
                Assert.True(HasLeadingZeros(new byte[2] { 0x00, 0x80 }, bits));
            }

            Assert.False(HasLeadingZeros(new byte[2] { 0x00, 0x80 }, 9));
            Assert.True(HasLeadingZeros(new byte[2] { 0x00, 0x7f }, 9));
            Assert.False(HasLeadingZeros(new byte[2] { 0x00, 0x7f }, 10));
            Assert.True(HasLeadingZeros(new byte[2] { 0x00, 0x20 }, 10));
        }

        private bool HasLeadingZeros(byte[] bytes, uint bits)
        {
            byte[] digest;
            if (bytes.Length < HashDigest<SHA256>.Size)
            {
                digest = new byte[HashDigest<SHA256>.Size];
                for (int i = 0; i < bytes.Length; i++)
                {
                    digest[i] = bytes[i];
                }
            }
            else
            {
                digest = bytes;
            }

            return new HashDigest<SHA256>(digest).HasLeadingZeroBits(bits);
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    internal class HashcashTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            for (int bits = 1; bits < 20; bits += 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    var challenge = TestUtils.GetRandomBytes(40);
                    yield return new object[] { challenge, bits };
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
