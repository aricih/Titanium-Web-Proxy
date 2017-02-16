using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Compression
{
	/// <summary>
	/// An inteface for http compression
	/// </summary>
	internal interface ICompression
	{
		Task<byte[]> Compress(byte[] responseBody, CancellationToken cancellationToken = default(CancellationToken));
	}
}
