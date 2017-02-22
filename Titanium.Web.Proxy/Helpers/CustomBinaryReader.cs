using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Extensions;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// A custom binary reader that would allo us to read string line by line
	/// using the specified encoding
	/// as well as to read bytes as required
	/// </summary>
	internal class CustomBinaryReader : IDisposable
	{
		private readonly Stream _stream;
		private readonly Encoding _encoding;

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomBinaryReader"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		internal CustomBinaryReader(Stream stream)
		{
			_stream = stream;

			//default to UTF-8
			_encoding = Encoding.UTF8;
		}

		/// <summary>
		/// Gets the base stream.
		/// </summary>
		internal Stream BaseStream => _stream;

		/// <summary>
		/// Read a line from the byte stream
		/// </summary>
		/// <returns></returns>
		internal async Task<string> ReadLineAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var readBuffer = new MemoryStream())
			{
				var lastChar = default(char);
				var buffer = new byte[1];
				
				while (await _stream.ReadAsync(buffer, 0, 1) > 0)
				{
					//if new line
					if (lastChar == '\r' && buffer[0] == '\n')
					{
						var result = readBuffer.ToArray();
						return _encoding.GetString(result.SubArray(0, result.Length - 1));
					}
					//end of stream
					if (buffer[0] == '\0')
					{
						return _encoding.GetString(readBuffer.ToArray());
					}

					await readBuffer.WriteAsync(buffer, 0, 1);

					//store last char for new line comparison
					lastChar = (char)buffer[0];
				}

				return _encoding.GetString(readBuffer.ToArray());
			}
		}

		/// <summary>
		/// Read until the last new line
		/// </summary>
		/// <returns></returns>
		internal async Task<List<string>> ReadAllLinesAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			string tmpLine;
			var requestLines = new List<string>();
			while (!string.IsNullOrEmpty(tmpLine = await ReadLineAsync(cancellationToken: cancellationToken)))
			{
				requestLines.Add(tmpLine);
			}
			return requestLines;
		}

		/// <summary>
		/// Read the specified number of raw bytes from the base stream
		/// </summary>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="totalBytesToRead">The total bytes to read.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Read bytes as byte array.</returns>
		internal async Task<byte[]> ReadBytesAsync(int bufferSize, long totalBytesToRead, CancellationToken cancellationToken = default(CancellationToken))
		{
			var bytesToRead = bufferSize;

			if (totalBytesToRead < bufferSize)
				bytesToRead = (int)totalBytesToRead;

			var buffer = new byte[bufferSize];

			var bytesRead = 0;
			var totalBytesRead = 0;

			using (var outStream = new MemoryStream())
			{
				while ((bytesRead += await _stream.ReadAsync(buffer, 0, bytesToRead, cancellationToken: cancellationToken)) > 0)
				{
					await outStream.WriteAsync(buffer, 0, bytesRead, cancellationToken: cancellationToken);
					totalBytesRead += bytesRead;

					if (totalBytesRead == totalBytesToRead)
						break;

					bytesRead = 0;
					var remainingBytes = totalBytesToRead - totalBytesRead;
					bytesToRead = remainingBytes > bufferSize ? bufferSize : (int)remainingBytes;
				}

				return outStream.ToArray();
			}

		}

		public void Dispose()
		{
		}
	}
}