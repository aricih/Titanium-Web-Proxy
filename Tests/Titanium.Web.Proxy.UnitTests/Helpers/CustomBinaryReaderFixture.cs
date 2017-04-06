using System;
using System.Collections;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.UnitTests.Helpers
{
	[TestFixture]
	public class CustomBinaryReaderFixture
	{
		private const int BufferSize = 16;
		private static readonly CancellationToken _cancelledToken = new CancellationToken(true);
		private static readonly CancellationToken _nonCancelledToken = new CancellationToken(false);
		private static readonly byte[] _infamousBytes = { 4, 8, 15, 16, 23, 42 };

		[TestCaseSource(typeof(CustomBinaryReaderTestCaseSource), nameof(CustomBinaryReaderTestCaseSource.ReadBytesAsyncTestCases))]
		public byte[] ReadBytesAsync_works_properly(byte[] inputData, int bufferSize, long totalBytesToRead, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var stream = new MemoryStream(inputData))
			using (var streamReader = new CustomBinaryReader(stream))
			{
				return streamReader.ReadBytesAsync(bufferSize, totalBytesToRead, cancellationToken).Result;
			}
		}

		private class CustomBinaryReaderTestCaseSource
		{
			public static IEnumerable ReadBytesAsyncTestCases
			{
				get
				{
					yield return new TestCaseData(Array.Empty<byte>(), BufferSize, 1, _nonCancelledToken)
						.Returns(Array.Empty<byte>())
						.SetName("Handles empty input stream properly");

					yield return new TestCaseData(_infamousBytes, BufferSize, 32, _nonCancelledToken)
						.Returns(_infamousBytes)
						.SetName("Handles insufficient input stream properly");

					yield return new TestCaseData(_infamousBytes, BufferSize, 0, _nonCancelledToken)
						.Returns(Array.Empty<byte>())
						.SetName("Handles zero bytes to read properly");

					yield return new TestCaseData(_infamousBytes, 0, 1, _nonCancelledToken)
						.Returns(Array.Empty<byte>())
						.SetName("Handles zero buffer size properly");

					yield return new TestCaseData(_infamousBytes, BufferSize, 1, _nonCancelledToken)
						.Returns(new byte[] { 4 })
						.SetName("Reads single byte properly");

					yield return new TestCaseData(_infamousBytes, BufferSize, _infamousBytes.Length, _nonCancelledToken)
						.Returns(_infamousBytes)
						.SetName("Reads all bytes properly");
				}
			}
		}
	}
}