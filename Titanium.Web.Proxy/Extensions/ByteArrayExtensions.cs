using System;

namespace Titanium.Web.Proxy.Extensions
{
	public static class ByteArrayExtensions
	{
		/// <summary>
		/// Get the sub array from byte of data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static T[] SubArray<T>(this T[] data, int index, int length)
		{
			if (index < 0 || length < 0)
			{
				return null;
			}

			if (index > data.Length - 1)
			{
				return null;
			}

			// Trim length parameter to prevent out of bound access
			length = index + length > data.Length
				? length % data.Length + 1
				: length;

			var result = new T[length];

			if (length == 0)
			{
				return result;
			}

			Array.Copy(data, index, result, 0, length);

			return result;
		}

	}
}
