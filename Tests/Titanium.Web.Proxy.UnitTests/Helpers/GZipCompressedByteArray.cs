using System.Linq;

namespace UnitTests.Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Wraps GZip compressed byte array
	/// </summary>
	public class GZipCompressedByteArray
	{
		private const int HeaderLength = 10;

		/// <summary>
		/// Single instance of GZip data with only header (magic number + version number + timestamp + payload + crc32)
		/// </summary>
		/// <remarks>https://en.wikipedia.org/wiki/Gzip</remarks>
		public static readonly GZipCompressedByteArray Empty = new GZipCompressedByteArray(31, 139, 8, 0, 129, 157, 98, 88, 0, 255);

		private byte[] gzipCompressedData;

		/// <summary>
		/// Initializes a new instance of the <see cref="GZipCompressedByteArray"/> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public GZipCompressedByteArray(params byte[] data)
		{
			gzipCompressedData = data;
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		public byte[] Value => gzipCompressedData;

		/// <summary>
		/// Gets the payload.
		/// </summary>
		private byte[] Payload => gzipCompressedData.SkipWhile((data, index) => index < HeaderLength).ToArray();

		/// <summary>
		/// Gets a value indicating whether this instance is empty.
		/// </summary>
		/// <remarks>Checks magic numbers according to: https://en.wikipedia.org/wiki/Gzip </remarks>
		private bool IsEmpty => gzipCompressedData.Length == HeaderLength
							   && gzipCompressedData[0] == 31
							   && gzipCompressedData[1] == 139
							   && gzipCompressedData[2] == 8
							   && gzipCompressedData[3] == 0;

		/// <summary>
		/// Checks whether this <see cref="GZipCompressedByteArray"/> equals to <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The other <see cref="GZipCompressedByteArray"/> instance to check against.</param>
		/// <returns><c>true</c> if instances are equal, <c>false</c> otherwise.</returns>
		private bool Equals(GZipCompressedByteArray other)
		{
			if (IsEmpty && other.IsEmpty)
			{
				return true;
			}

			return Value.Length == other.Value.Length
			       && Payload.SequenceEqual(other.Payload);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((GZipCompressedByteArray)obj);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode()
		{
			return gzipCompressedData.GetHashCode();
		}
	}

}