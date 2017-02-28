using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Compression
{
	/// <summary>
	///  A factory to generate the compression methods based on the type of compression
	/// </summary>
	internal class CompressionFactory
	{
		/// <summary>
		/// Creates the specified type.
		/// </summary>
		/// <param name="type">The compression type.</param>
		/// <returns>Compression instance.</returns>
		public ICompression Create(string type)
		{
			switch (type)
			{
				case CompressionConstants.GZipCompression:
					return new GZipCompression();
				case CompressionConstants.DeflateCompression:
					return new DeflateCompression();
				case CompressionConstants.ZlibCompression:
					return new ZlibCompression();
				default:
					return null;
			}
		}
	}
}
