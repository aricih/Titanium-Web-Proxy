using System.IO;
using NUnit.Framework;
using Titanium.Web.Proxy.Extensions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.UnitTests.Extensions
{
	[TestFixture]
	public class StreamExtensionsFixture
	{
		private StreamWriter _writer;
		private StreamReader _reader;

		private StreamWriter Writer
		{
			get
			{
				return _writer;
			}
			set
			{
				_writer?.Dispose();
				_writer = value;
			}
		}

		private StreamReader Reader
		{
			get
			{
				return _reader;
			}
			set
			{
				_reader?.Dispose();
				_reader = value;
			}
		}

		/// <summary>
		/// Generates the stream from string.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <returns>Stream generated from the given string.</returns>
		private Stream GetStreamFromString(string data)
		{
			var stream = new MemoryStream();

			Writer = new StreamWriter(stream);
			Writer.Write(data);
			Writer.Flush();

			stream.Position = 0;

			return stream;
		}

		/// <summary>
		/// Gets the custom binary reader from string.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <returns>CustomBinaryReader instance to read created stream from the given string.</returns>
		private CustomBinaryReader GetCustomBinaryReaderFromString(string data)
		{
			return new CustomBinaryReader(GetStreamFromString(data));
		}

		/// <summary>
		/// Reads the string from stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>String read from the given stream.</returns>
		private string ReadStringFromStream(Stream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);
			Reader = new StreamReader(stream);
			return Reader.ReadToEnd();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_reader?.Dispose();
			_writer?.Dispose();
		}

		[TestCase(null, null, ExpectedResult = "", TestName = "Handles null input")]
		[TestCase("", "", ExpectedResult = "", TestName = "Handles empty input with empty initial output data")]
		[TestCase("", "TestData", ExpectedResult = "TestData", TestName = "Handles empty input with arbitrary initial output data")]
		[TestCase("TestData", "", ExpectedResult = "TestData", TestName = "Handles arbitrary input with empty initial output data")]
		[TestCase("Data", "Test", ExpectedResult = "TestData", TestName = "Handles arbitrary input with arbitrary initial output data")]
		public string CopyToAsync_works_properly(string inputData, string initialOutputData)
		{
			using (var stream = GetStreamFromString(inputData))
			using (var outputStream = new MemoryStream())
			{
				stream.CopyToAsync(initialOutputData, outputStream).ConfigureAwait(false);

				return ReadStringFromStream(outputStream);
			}
		}

		[TestCase(null, 0, 0, ExpectedResult = "", TestName = "Handles empty input stream with zero buffer and zero bytes to read")]
		[TestCase(null, 0, 10, ExpectedResult = "", TestName = "Handles empty input stream with zero buffer and non-zero bytes to read")]
		[TestCase(null, 8, 10, ExpectedResult = "", TestName = "Handles empty input stream with proper buffer and non-zero bytes to read")]
		[TestCase("TestData", 8, -1, ExpectedResult = "", TestName = "Handles negative bytes to read value")]
		[TestCase("TestData", -1, 1, ExpectedResult = "", TestName = "Handles negative buffer size vaue")]
		[TestCase("TestData", 8, 4, ExpectedResult = "Test", TestName = "Copies bytes properly with proper buffer size and proper bytes to read values")]
		[TestCase("TestData", 8, 10, ExpectedResult = "TestData", TestName = "Handles more bytes to read value that the source data's byte length")]
		public string CopyBytesToStream_works_properly(string inputData, int bufferSize, long totalBytesToRead)
		{
			using (var inputStream = GetStreamFromString(inputData))
			using (var outputStream = new MemoryStream())
			using (var customBinaryReader = new CustomBinaryReader(inputStream))
			{
				customBinaryReader.CopyBytesToStream(bufferSize, outputStream, totalBytesToRead).ConfigureAwait(false);

				return ReadStringFromStream(outputStream);
			}
		}

		[TestCase(null, 0, ExpectedResult = "", TestName = "Handles empty input stream")]
		[TestCase("01\r\nTest", 0, ExpectedResult = "", TestName = "Handles zero buffer size with proper chunked data")]
		[TestCase("04\r\nTest", 4, ExpectedResult = "Test", TestName = "Handles single chunked data")]
		[TestCase("04\r\nTest\r\n04\r\nData", 4, ExpectedResult = "TestData", TestName = "Handles multiple chunked data")]
		[TestCase("03\r\nTest\r\n02\r\nData", 4, ExpectedResult = "TesDa", TestName = "Handles different chunk sizes")]
		[TestCase("04\r\nTest\r\n04\r\nData\r\n00\r\n\r\n", 4, ExpectedResult = "TestData", TestName = "Handles multiple chunked data with empty chunk at the end")]
		public string CopyBytesToStreamChunked_works_properly(string inputData, int bufferSize)
		{
			using (var inputStream = GetStreamFromString(inputData))
			using (var customBinaryReader = new CustomBinaryReader(inputStream))
			using (var outputStream = new MemoryStream())
			{
				customBinaryReader.CopyBytesToStreamChunked(bufferSize, outputStream).ConfigureAwait(false);

				return ReadStringFromStream(outputStream);
			}
		}

		[TestCase(null, false, ExpectedResult = "", TestName = "Handles empty response data")]
		[TestCase("TestData", false, ExpectedResult = "TestData", TestName = "Handles arbitrary response data")]
		[TestCase("TestData", true, ExpectedResult = "08\r\nTestData\r\n00\r\n\r\n", TestName = "Handles chunked response data")]
		public string WriteResponseBody_as_Stream_extension_works_properly(string responseData, bool isChunked)
		{
			using (var clientStream = new MemoryStream())
			{
				clientStream.WriteResponseBody(ProxyConstants.DefaultEncoding.GetBytes(responseData ?? string.Empty), isChunked).ConfigureAwait(false);

				return ReadStringFromStream(clientStream);
			}
		}

		[TestCase(null, 0, false, 0, ExpectedResult = "", TestName = "Handles empty stream")]
		[TestCase("TestData", 0, false, 8, ExpectedResult = "", TestName = "Handles zero buffer size")]
		[TestCase("TestData", 8, false, 0, ExpectedResult = "", TestName = "Handles zero content length")]
		[TestCase("TestData", 8, false, 8, ExpectedResult = "TestData", TestName = "Handles arbitrary input data properly")]
		[TestCase("08\r\nTestData\r\n00\r\n\r\n", 8, true, 8, ExpectedResult = "08\r\nTestData\r\n00\r\n\r\n", TestName = "Handles chunked input data properly")]
		public string WriteResponseBody_as_CustomBinaryReader_extension_works_properly(string inputData, int bufferSize, bool isChunked,
			long contentLength)
		{
			using (var customBinaryReader = GetCustomBinaryReaderFromString(inputData))
			using (var outputStream = new MemoryStream())
			{
				customBinaryReader.WriteResponseBody(bufferSize, outputStream, isChunked, contentLength).ConfigureAwait(false);

				return ReadStringFromStream(outputStream);
			}
		}

		[TestCase(null, 0, ExpectedResult = "", TestName = "Handles empty stream (chunked)")]
		[TestCase("TestData", 0, ExpectedResult = "", TestName = "Handles zero buffer size (chunked)")]
		[TestCase("08\r\nTestData\r\n00\r\n\r\n", 8, ExpectedResult = "08\r\nTestData\r\n00\r\n\r\n", TestName = "Handles single chunked input data with proper buffer")]
		[TestCase("04\r\nTest\r\n04\r\nData\r\n00\r\n\r\n", 4, ExpectedResult = "04\r\nTest\r\n04\r\nData\r\n00\r\n\r\n", TestName = "Handles multiple chunked input data with proper buffer")]
		[TestCase("03\r\nTest\r\n02\r\nData\r\n00\r\n\r\n", 4, ExpectedResult = "03\r\nTes\r\n02\r\nDa\r\n00\r\n\r\n", TestName = "Handles multiple chunked input data with different chunk sizes and proper buffer")]
		public string WriteResponseBodyChunked_as_CustomBinaryReader_extension_works_properly(string inputData, int bufferSize)
		{
			using (var customBinaryReader = GetCustomBinaryReaderFromString(inputData))
			using (var outputStream = new MemoryStream())
			{
				customBinaryReader.WriteResponseBodyChunked(bufferSize, outputStream).ConfigureAwait(false);

				return ReadStringFromStream(outputStream);
			}
		}

		[TestCase(null, ExpectedResult = "00\r\n\r\n00\r\n\r\n", TestName = "Handles empty byte array, results with empty chunks")]
		[TestCase("TestData", ExpectedResult = "08\r\nTestData\r\n00\r\n\r\n", TestName = "Handles arbitrary byte array")]
		public string WriteResponseBodyChunked_as_byte_array_extension_works_properly(string inputData)
		{
			using (var outputStream = new MemoryStream())
			{
				ProxyConstants.DefaultEncoding.GetBytes(inputData ?? string.Empty).WriteResponseBodyChunked(outputStream).ConfigureAwait(false);

				return ReadStringFromStream(outputStream);
			}
		}
	}
}