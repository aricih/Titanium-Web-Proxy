using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Decompression
{
	/// <summary>
	/// An interface for decompression
	/// </summary>
	internal interface IDecompression
	{
		/// <summary>
		/// Decompresses the specified compressed stream.
		/// </summary>
		/// <param name="compressedStream">The compressed stream.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Decompressed data as memory stream.</returns>
		Task<MemoryStream> Decompress(MemoryStream compressedStream, int bufferSize, CancellationToken cancellationToken = default(CancellationToken));
	}
}
