using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Decompression
{
	/// <summary>
	/// When no compression is specified just return the byte array
	/// </summary>
	internal class DefaultDecompression : IDecompression
	{
		public Task<byte[]> Decompress(byte[] compressedArray, int bufferSize, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromResult(compressedArray);
		}
	}
}
