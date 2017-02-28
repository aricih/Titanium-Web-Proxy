using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Titanium.Web.Proxy.Decompression;

namespace Titanium.Web.Proxy.UnitTests.Decompression
{
	[TestFixture]
	public class ZlibDecompressionFixture
	{
		private const int BufferSize = 16;

		[TestCaseSource(typeof(ZlibDecompressionTestCaseSource), nameof(ZlibDecompressionTestCaseSource.DecompressTestCases))]
		public byte[] Decompress_works_properly(byte[] compressedData, int bufferSize)
		{
			var decompression = new ZlibDecompression();

			// Hack: If compressed data is null then don't use the constructed memory stream, pass null to Decompress instead
			using (var inputStream = new MemoryStream(compressedData ?? new byte[0]))
			{
				return decompression.Decompress(compressedData != null ? inputStream : null, bufferSize).Result?.ToArray();
			}
		}

		private class ZlibDecompressionTestCaseSource
		{
			public static IEnumerable DecompressTestCases
			{
				get
				{
					yield return new TestCaseData(null, BufferSize)
						.Returns(null)
						.SetName("Handles null input");

					yield return new TestCaseData(Array.Empty<byte>(), BufferSize)
						.Returns(Array.Empty<byte>())
						.SetName("Handles empty input");

					yield return new TestCaseData(Array.Empty<byte>(), 0)
						.Returns(null)
						.SetName("Handles invalid buffer size");

					yield return new TestCaseData(new byte[] { 120, 156, 99, 96, 128, 0, 0, 0, 8, 0, 1 }, BufferSize)
						.Returns(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })
						.SetName("Decompresses zero vector properly");

					yield return new TestCaseData(new byte[] { 120, 156, 99, 96, 100, 98, 225, 16, 80, 112, 0, 0, 0, 255, 0, 128 }, BufferSize)
						.Returns(new byte[] { 0, 1, 2, 4, 8, 16, 32, 64 })
						.SetName("Decompresses arbitrary data properly");
				}
			}
		}
	}
}