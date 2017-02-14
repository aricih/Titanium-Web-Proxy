using System;
using NUnit.Framework;
using Titanium.Web.Proxy.Compression;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.UnitTests.Compression
{
    [TestFixture]
    public class CompressionFactoryFixture
    {
        [TestCase(null, null, TestName = "Handles null input")]
        [TestCase("", null, TestName = "Handles empty input")]
        [TestCase("dummy", null, TestName = "Handles invalid input")]
        [TestCase(CompressionConstants.GZipCompression, typeof(GZipCompression), TestName = "Creates GZipCompression instance properly")]
        [TestCase(CompressionConstants.DeflateCompression, typeof(DeflateCompression), TestName = "Creates DeflateCompression instance properly")]
        [TestCase(CompressionConstants.ZlibCompression, typeof(ZlibCompression), TestName = "Creates ZlibCompression instance properly")]
        public void Create_works_properly(string compressionType, Type expectedType)
        {
            var compressionFactory = new CompressionFactory();

            Assert.AreEqual(compressionFactory.Create(compressionType)?.GetType(), expectedType);
        }
    }
}