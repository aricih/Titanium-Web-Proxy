﻿using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Decompression
{
    /// <summary>
    /// concrete implementation of gzip de-compression
    /// </summary>
    internal class GZipDecompression : IDecompression
    {
        public async Task<byte[]> Decompress(byte[] compressedArray, int bufferSize)
        {
            if (compressedArray == null || bufferSize < 1)
            {
                return null;
            }

            using (var decompressor = new GZipStream(new MemoryStream(compressedArray), CompressionMode.Decompress))
            {
                var buffer = new byte[bufferSize];
                using (var output = new MemoryStream())
                {
                    int read;
                    while ((read = await decompressor.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                       await output.WriteAsync(buffer, 0, read);
                    }
                    return output.ToArray();
                }
            }
        }
    }
}
