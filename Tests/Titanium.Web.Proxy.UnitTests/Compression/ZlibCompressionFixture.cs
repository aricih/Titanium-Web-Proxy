using System;
using System.Collections;
using NUnit.Framework;
using Titanium.Web.Proxy.Compression;

namespace Titanium.Web.Proxy.UnitTests.Compression
{
    [TestFixture]
    public class ZlibCompressionFixture
    {
        [TestCaseSource(typeof(ZlibCompressionTestCaseSource), nameof(ZlibCompressionTestCaseSource.CompressTestCases))]
        public byte[] Compress_works_properly(byte[] data)
        {
            var compression = new ZlibCompression();

            return compression.Compress(data).Result;
        }

        private class ZlibCompressionTestCaseSource
        {
            public static IEnumerable CompressTestCases
            {
                get
                {
                    yield return new TestCaseData(null)
                        .Returns(null)
                        .SetName("Handles null input");

                    yield return new TestCaseData(Array.Empty<byte>())
                        .Returns(Array.Empty<byte>())
                        .SetName("Handles empty input");

                    yield return new TestCaseData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })
                        .Returns(new byte[] { 120, 156, 99, 96, 128, 0, 0, 0, 8, 0, 1 })
                        .SetName("new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }");

                    yield return new TestCaseData(new byte[] { 0, 1, 2, 4, 8, 16, 32, 64 })
                        .Returns(new byte[] { 120, 156, 99, 96, 100, 98, 225, 16, 80, 112, 0, 0, 0, 255, 0, 128 })
                        .SetName("Compresses arbitrary data properly");
                }
            }
        }
    }
}