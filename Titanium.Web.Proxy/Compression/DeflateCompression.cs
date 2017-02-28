using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Compression
{
	/// <summary>
	/// Concrete implementation of deflate compression
	/// </summary>
	internal class DeflateCompression : ICompression
	{
		/// <summary>
		/// Compresses the specified response body.
		/// </summary>
		/// <param name="responseBody">The response body.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Compressed data as memory stream.</returns>
		public async Task<MemoryStream> Compress(byte[] responseBody, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (responseBody == null)
			{
				return null;
			}

			var compressedStream = new MemoryStream();

			using (var zip = new DeflateStream(compressedStream, CompressionMode.Compress, true))
			{
				await zip.WriteAsync(responseBody, 0, responseBody.Length, cancellationToken: cancellationToken);
			}

			if (!cancellationToken.IsCancellationRequested)
			{
				return compressedStream;
			}

			compressedStream.Dispose();
			return null;

		}
	}
}
