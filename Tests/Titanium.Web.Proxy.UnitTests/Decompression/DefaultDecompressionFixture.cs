using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Titanium.Web.Proxy.Decompression;

namespace Titanium.Web.Proxy.UnitTests.Decompression
{
	[TestFixture]
	public class DefaultDecompressionFixture
	{
		[TestCaseSource(typeof(DefaultDecompressionTestCaseSource), nameof(DefaultDecompressionTestCaseSource.DecompressTestCases))]
		public byte[] Decompress_works_properly(byte[] compressedData)
		{
			var decompression = new DefaultDecompression();

			// Hack: If compressed data is null then don't use the constructed memory stream, pass null to Decompress instead
			using (var inputStream = new MemoryStream(compressedData ?? new byte[0]))
			{
				return decompression.Decompress(compressedData != null ? inputStream : null, 0).Result?.ToArray();
			}
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