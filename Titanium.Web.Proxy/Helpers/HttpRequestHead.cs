using System;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Represents the first line of a HTTP request.
	/// </summary>
	internal class HttpRequestHead
	{
		/// <summary>
		/// Gets or sets the method.
		/// </summary>
		internal string Method { get; set; }

		/// <summary>
		/// Gets or sets the URL.
		/// </summary>
		internal string Url { get; set; }

		/// <summary>
		/// Gets or sets the version.
		/// </summary>
		internal Version Version { get; set; }
	}
}