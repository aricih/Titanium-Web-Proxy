using System;
using NUnit.Framework;
using Titanium.Web.Proxy.Decompression;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.UnitTests.Decompression
{
    [TestFixture]
    public class DecompressionFactoryFixture
    {
        [TestCase(null, typeof(DefaultDecompression), TestName = "Handles null input, creates DefaultDecompression instance")]
        [TestCase("", typeof(DefaultDecompression), TestName = "Handles empty input, creates DefaultDecompression instance")]
        [TestCase("dummy", typeof(DefaultDecompression), TestName = "Handles invalid input, creates DefaultDecompression instance")]
        [TestCase(CompressionConstants.GZipCompression, typeof(GZipDecompression), TestName = "Creates GZipDecompression instance properly")]
        [TestCase(CompressionConstants.DeflateCompression, typeof(DeflateDecompression), TestName = "Creates DeflateDecompression instance properly")]
        [TestCase(CompressionConstants.ZlibCompression, typeof(ZlibDecompression), TestName = "Creates ZlibDecompression instance properly")]
        public void Create_works_properly(string compressionType, Type expectedType)
        {
            var compressionFactory = new DecompressionFactory();

            Assert.AreEqual(compressionFactory.Create(compressionType)?.GetType(), expectedType);
        }
    }
}