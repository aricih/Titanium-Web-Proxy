using System.IO;
using System.Text;
using Titanium.Web.Proxy.Extensions;
using System;

namespace Titanium.Web.Proxy.Http
{
	/// <summary>
	/// Http(s) response object
	/// </summary>
	public class Response : HttpMessage
	{
		/// <summary>
		/// Gets or sets the response status code.
		/// </summary>
		public int StatusCode { get; set; }

		/// <summary>
		/// Gets or sets the response status description.
		/// </summary>
		public string StatusDescription { get; set; }

		/// <summary>
		/// Gets the response done timestamp.
		/// </summary>
		public DateTime ResponseReceived { get; internal set; }

		/// <summary>
		/// Keep the connection alive?
		/// </summary>
		internal bool KeepAlive
		{
			get
			{
				var hasHeader = Headers.ContainsKey("connection");

				if (!hasHeader)
				{
					return true;
				}

				var header = Headers["connection"];

				return !header.Value.ToLower().Contains("close");
			}
		}

		/// <summary>
		/// Response network stream
		/// </summary>
		public Stream NetworkStream { get; set; }

		/// <summary>
		/// Is response 100-continue
		/// </summary>
		public bool Is100Continue { get; internal set; }

		/// <summary>
		/// expectation failed returned by server?
		/// </summary>
		public bool ExpectationFailed { get; internal set; }
	}

}
