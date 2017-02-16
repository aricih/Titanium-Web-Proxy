using Ionic.Zlib;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Decompression
{
	/// <summary>
	/// concrete implementation of deflate de-compression
	/// </summary>
	internal class DeflateDecompression : IDecompression
	{
		/// <summary>
		/// Decompresses the specified compressed array.
		/// </summary>
		/// <param name="compressedArray">The compressed array.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Decompressed data as byte array.</returns>
		public async Task<byte[]> Decompress(byte[] compressedArray, int bufferSize, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (compressedArray == null || bufferSize < 1)
			{
				return null;
			}

			var stream = new MemoryStream(compressedArray);

			using (var decompressor = new DeflateStream(stream, CompressionMode.Decompress))
			{
				var buffer = new byte[bufferSize];

				using (var output = new MemoryStream())
				{
					int read;

					while ((read = await decompressor.ReadAsync(buffer, 0, buffer.Length, cancellationToken: cancellationToken)) > 0)
					{
						await output.WriteAsync(buffer, 0, read, cancellationToken: cancellationToken);
					}

					return cancellationToken.IsCancellationRequested ? null : output.ToArray();
				}
			}
		}
	}
}
