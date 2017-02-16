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
		public async Task<byte[]> Decompress(byte[] compressedArray, int bufferSize, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (compressedArray == null || bufferSize < 1)
			{
				return null;
			}

			var memoryStream = new MemoryStream(compressedArray);

			using (var decompressor = new ZlibStream(memoryStream, CompressionMode.Decompress))
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

