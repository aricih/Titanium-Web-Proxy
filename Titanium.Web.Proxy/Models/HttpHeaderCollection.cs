using System.Collections.Generic;

namespace Titanium.Web.Proxy.Models
{
	/// <summary>
	/// Implements a collection class for HttpHeader.
	/// </summary>
	public class HttpHeaderCollection : Dictionary<string, HttpHeader>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HttpHeaderCollection"/> class.
		/// </summary>
		public HttpHeaderCollection()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpHeaderCollection"/> class.
		/// </summary>
		/// <param name="headers">The headers.</param>
		public HttpHeaderCollection(IEnumerable<HttpHeader> headers)
		{
			if (headers == null)
			{
				return;
			}

			foreach (var httpHeader in headers)
			{
				Add(httpHeader);
			}
		}

		/// <summary>
		/// Adds the specified header, overwrites if the same header exists.
		/// </summary>
		/// <param name="header">The header.</param>
		public void Add(HttpHeader header)
		{
			if (header == null)
			{
				return;
			}

			this[header.Name] = header;
		}
	}
}