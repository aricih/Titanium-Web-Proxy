using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Decompression
{
	/// <summary>
	/// When no compression is specified just return the byte array
	/// </summary>
	internal class DefaultDecompression : IDecompression
	{
		/// <summary>
		/// Decompresses the specified compressed array.
		/// </summary>
		/// <param name="compressedArray">The compressed array.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Decompressed data as memory stream.</returns>
		public Task<MemoryStream> Decompress(MemoryStream compressedArray, int bufferSize, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromResult(compressedArray);
		}
	}
}
