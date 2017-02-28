using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Compression
{
	/// <summary>
	/// An inteface for http compression
	/// </summary>
	internal interface ICompression
	{
		/// <summary>
		/// Compresses the specified response body.
		/// </summary>
		/// <param name="responseBody">The response body.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Compressed data as memory stream.</returns>
		Task<MemoryStream> Compress(byte[] responseBody, CancellationToken cancellationToken = default(CancellationToken));
	}
}
