using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Decompression
{
    /// <summary>
    /// A factory to generate the de-compression methods based on the type of compression
    /// </summary>
    internal class DecompressionFactory
    {
        internal IDecompression Create(string type)
        {
            switch(type)
            {
                case CompressionConstants.GZipCompression:
                    return new GZipDecompression();
                case CompressionConstants.DeflateCompression:
                    return new DeflateDecompression();
                case CompressionConstants.ZlibCompression:
                    return new ZlibDecompression();
                default:
                    return new DefaultDecompression();
            }
        }
    }
}
