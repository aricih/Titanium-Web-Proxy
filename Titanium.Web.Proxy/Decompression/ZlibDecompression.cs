using Ionic.Zlib;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Decompression
{
	/// <summary>
	/// concrete implemetation of zlib de-compression
	/// </summary>
	internal class ZlibDecompression : IDecompression
	{
		/// <summary>
		/// Decompresses the specified compressed stream.
		/// </summary>
		/// <param name="compressedStream">The compressed stream.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Decompressed data as memory stream.</returns>
		public async Task<MemoryStream> Decompress(MemoryStream compressedStream, int bufferSize, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (compressedStream == null || bufferSize < 1)
			{
				return null;
			}

			using (var decompressor = new ZlibStream(compressedStream, CompressionMode.Decompress, true))
			{
				var buffer = new byte[bufferSize];
				var output = new MemoryStream();

				int read;
				var totalBytesRead = 0;

				while ((read = await decompressor.ReadAsync(buffer, 0, buffer.Length, cancellationToken: cancellationToken)) > 0)
				{
					totalBytesRead += read;

					if (ProxyServer.Instance.AbortAtMaximumResponseSize
						&& totalBytesRead > ProxyServer.Instance.MaximumResponseSizeAsBytes)
					{
						return null;
					}

					await output.WriteAsync(buffer, 0, read, cancellationToken: cancellationToken);
				}

				if (cancellationToken.IsCancellationRequested)
				{
					output.Dispose();
				}

				return cancellationToken.IsCancellationRequested ? null : output;
			}
		}
	}
}