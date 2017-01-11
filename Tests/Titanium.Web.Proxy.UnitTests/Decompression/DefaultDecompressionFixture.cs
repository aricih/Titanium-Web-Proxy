using System;
using System.Collections;
using NUnit.Framework;
using Titanium.Web.Proxy.Decompression;

namespace Titanium.Web.Proxy.UnitTests.Decompression
{
    [TestFixture]
    public class DefaultDecompressionFixture
    {
        [TestCaseSource(typeof(DefaultDecompressionTestCaseSource), nameof(DefaultDecompressionTestCaseSource.DecompressTestCases))]
        public byte[] Decompress_works_properly(byte[] data)
        {
            var decompression = new DefaultDecompression();

            return decompression.Decompress(data, 0).Result;
        }

        private class DefaultDecompressionTestCaseSource
        {
            public static IEnumerable DecompressTestCases
            {
                get
                {
                    yield return new TestCaseData(null)
                        .Returns(null)
                        .SetName("Handles null input");

                    yield return new TestCaseData(Array.Empty<byte>())
                        .Returns(Array.Empty<byte>())
                        .SetName("Handles empty input");

                    yield return new TestCaseData(new byte[] { 78, 101, 116, 115, 112, 97, 114, 107, 101, 114 })
                        .Returns(new byte[] { 78, 101, 116, 115, 112, 97, 114, 107, 101, 114 })
                        .SetName("Handles any arbitrary input");
                }
            }
        }
    }
}