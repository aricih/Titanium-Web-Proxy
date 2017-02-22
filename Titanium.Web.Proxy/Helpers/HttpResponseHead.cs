using System;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Represents the first line of a HTTP response.
	/// </summary>
	internal struct HttpResponseHead
	{
		/// <summary>
		/// Gets or sets the version.
		/// </summary>
		internal Version Version { get; set; }

		/// <summary>
		/// Gets or sets the status code.
		/// </summary>
		internal int StatusCode { get; set; }

		/// <summary>
		/// Gets or sets the status description.
		/// </summary>
		internal string StatusDescription { get; set; }
	}
}