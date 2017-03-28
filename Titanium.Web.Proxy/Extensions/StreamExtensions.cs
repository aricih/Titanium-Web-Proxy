using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Extensions
{
	/// <summary>
	/// Extensions used for Stream and CustomBinaryReader objects
	/// </summary>
	internal static class StreamExtensions
	{
		/// <summary>
		/// Copy streams asynchronously with an initial data inserted to the beginning of stream
		/// </summary>
		/// <param name="input">The input.</param>
		/// <param name="initialData">The initial data.</param>
		/// <param name="output">The output.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task CopyToAsync(this Stream input, string initialData, Stream output, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!string.IsNullOrEmpty(initialData))
			{
				var bytes = ProxyConstants.DefaultEncoding.GetBytes(initialData);
				await output.WriteAsync(bytes, 0, bytes.Length, cancellationToken: cancellationToken);
			}

			await input.CopyToAsync(output);
		}

		/// <summary>
		/// copies the specified bytes to the stream from the input stream
		/// </summary>
		/// <param name="streamReader">The stream reader.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="totalBytesToRead">The total bytes to read.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task CopyBytesToStream(this CustomBinaryReader streamReader, int bufferSize, Stream stream, long totalBytesToRead, CancellationToken cancellationToken = default(CancellationToken))
		{
			var totalbytesRead = 0;

			var bytesToRead = totalBytesToRead < bufferSize ? totalBytesToRead : bufferSize;

			while (totalbytesRead < totalBytesToRead)
			{
				var buffer = await streamReader.ReadBytesAsync(bufferSize, bytesToRead, cancellationToken: cancellationToken);

				if (buffer.Length == 0)
				{
					break;
				}

				totalbytesRead += buffer.Length;

				var remainingBytes = totalBytesToRead - totalbytesRead;
				if (remainingBytes < bytesToRead)
				{
					bytesToRead = remainingBytes;
				}

				await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken: cancellationToken);
			}

			stream.Seek(0, SeekOrigin.Begin);
		}

		/// <summary>
		/// Copies the stream chunked
		/// </summary>
		/// <param name="clientStreamReader">The client stream reader.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task CopyBytesToStreamChunked(this CustomBinaryReader clientStreamReader, int bufferSize, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
		{
			while (true)
			{
				var chunkHead = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
				int chunkSize;

				if (!int.TryParse(chunkHead, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out chunkSize))
				{
					return;
				}

				if (chunkSize != 0)
				{
					var buffer = await clientStreamReader.ReadBytesAsync(bufferSize, chunkSize, cancellationToken: cancellationToken);
					await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken: cancellationToken);
					//chunk trail
					await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
				}
				else
				{
					await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
					break;
				}
			}

			stream.Seek(0, SeekOrigin.Begin);
		}

		/// <summary>
		/// Writes the byte array body to the given stream; optionally chunked
		/// </summary>
		/// <param name="clientStream">The client stream.</param>
		/// <param name="data">The data.</param>
		/// <param name="isChunked">if set to <c>true</c> [is chunked].</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task WriteResponseBody(this Stream clientStream, byte[] data, bool isChunked, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!isChunked)
			{
				await clientStream.WriteAsync(data, 0, data.Length, cancellationToken: cancellationToken);
			}
			else
			{
				await WriteResponseBodyChunked(data, clientStream, cancellationToken: cancellationToken);
			}
		}

		/// <summary>
		/// Copies the specified content length number of bytes to the output stream from the given inputs stream
		/// optionally chunked
		/// </summary>
		/// <param name="inStreamReader">The in stream reader.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="outStream">The out stream.</param>
		/// <param name="isChunked">if set to <c>true</c> [is chunked].</param>
		/// <param name="contentLength">Length of the content.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task WriteResponseBody(this CustomBinaryReader inStreamReader, int bufferSize, Stream outStream, bool isChunked, long contentLength, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (isChunked)
			{
				await WriteResponseBodyChunked(inStreamReader, bufferSize, outStream, cancellationToken: cancellationToken);
			}
			else
			{
				//http 1.0
				if (contentLength == -1)
				{
					contentLength = long.MaxValue;
				}

				var bytesToRead = bufferSize;

				if (contentLength < bufferSize)
				{
					bytesToRead = (int) contentLength;
				}

				var buffer = new byte[bufferSize];

				var bytesRead = 0;
				var totalBytesRead = 0;

				while (
				(bytesRead +=
					await inStreamReader.BaseStream.ReadAsync(buffer, 0, bytesToRead, cancellationToken: cancellationToken)) > 0)
				{
					await outStream.WriteAsync(buffer, 0, bytesRead, cancellationToken: cancellationToken);
					totalBytesRead += bytesRead;

					if (totalBytesRead == contentLength)
						break;

					bytesRead = 0;
					var remainingBytes = (contentLength - totalBytesRead);
					bytesToRead = remainingBytes > (long) bufferSize ? bufferSize : (int) remainingBytes;
				}
			}
		}

		/// <summary>
		/// Copies the streams chunked
		/// </summary>
		/// <param name="inStreamReader">The in stream reader.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="outStream">The out stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task WriteResponseBodyChunked(this CustomBinaryReader inStreamReader, int bufferSize, Stream outStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			while (true)
			{
				var chunkHead = await inStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
				int chunkSize;

				if (!int.TryParse(chunkHead, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out chunkSize))
				{
					return;
				}

				if (chunkSize != 0)
				{
					var buffer = await inStreamReader.ReadBytesAsync(bufferSize, chunkSize, cancellationToken: cancellationToken);

					var chunkHeadBytes = ProxyConstants.DefaultEncoding.GetBytes(chunkSize.ToString("x2"));

					await outStream.WriteAsync(chunkHeadBytes, 0, chunkHeadBytes.Length, cancellationToken: cancellationToken);
					await outStream.WriteAsync(ProxyConstants.NewLineBytes, 0, ProxyConstants.NewLineBytes.Length, cancellationToken: cancellationToken);

					await outStream.WriteAsync(buffer, 0, chunkSize, cancellationToken: cancellationToken);
					await outStream.WriteAsync(ProxyConstants.NewLineBytes, 0, ProxyConstants.NewLineBytes.Length, cancellationToken: cancellationToken);

					await inStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
				}
				else
				{
					await inStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
					await outStream.WriteAsync(ProxyConstants.ChunkEnd, 0, ProxyConstants.ChunkEnd.Length, cancellationToken: cancellationToken);
					break;
				}
			}
		}

		/// <summary>
		/// Copies the given input bytes to output stream chunked
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="outStream">The out stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task WriteResponseBodyChunked(this byte[] data, Stream outStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			var chunkHead = ProxyConstants.DefaultEncoding.GetBytes(data.Length.ToString("x2"));

			await outStream.WriteAsync(chunkHead, 0, chunkHead.Length, cancellationToken: cancellationToken);
			await outStream.WriteAsync(ProxyConstants.NewLineBytes, 0, ProxyConstants.NewLineBytes.Length, cancellationToken: cancellationToken);
			await outStream.WriteAsync(data, 0, data.Length, cancellationToken: cancellationToken);
			await outStream.WriteAsync(ProxyConstants.NewLineBytes, 0, ProxyConstants.NewLineBytes.Length, cancellationToken: cancellationToken);

			await outStream.WriteAsync(ProxyConstants.ChunkEnd, 0, ProxyConstants.ChunkEnd.Length, cancellationToken: cancellationToken);
		}
	}
}