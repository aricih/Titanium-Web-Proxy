using Ionic.Zlib;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Compression
{
	/// <summary>
	/// concreate implementation of gzip compression
	/// </summary>
	internal class GZipCompression : ICompression
	{
		public async Task<byte[]> Compress(byte[] responseBody, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (responseBody == null)
			{
				return null;
			}

			using (var ms = new MemoryStream())
			{
				using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
				{
					await zip.WriteAsync(responseBody, 0, responseBody.Length, cancellationToken: cancellationToken);
				}

				return cancellationToken.IsCancellationRequested ? null : ms.ToArray();
			}
		}
	}
}
