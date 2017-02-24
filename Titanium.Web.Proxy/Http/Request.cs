using System;
using System.Collections.Generic;
using System.Text;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Extensions;

namespace Titanium.Web.Proxy.Http
{
	/// <summary>
	/// A HTTP(S) request object
	/// </summary>
	public class Request : HttpMessage
	{
		/// <summary>
		/// Request Method
		/// </summary>
		public string Method { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether request body will be recorded.
		/// </summary>
		public bool RecordBody { get; set; }

		/// <summary>
		/// Request HTTP Uri
		/// </summary>
		public Uri TargetUri { get; set; }

		/// <summary>
		/// Gets the request begin timestamp.
		/// </summary>
		public DateTime RequestBegin { get; internal set; }

		/// <summary>
		/// Request Http hostanem
		/// </summary>
		internal string Host
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("host", out header);

				return header?.Value;
			}
			set
			{
				Headers["host"] = new HttpHeader("host", value);
			}
		}

		/// <summary>
		/// Does this request has a 100-continue header?
		/// </summary>
		public bool ExpectContinue
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("expect", out header);

				return header?.Value?.Equals("100-continue", StringComparison.OrdinalIgnoreCase) ?? false;
			}
		}

		/// <summary>
		/// Request Url
		/// </summary>
		public string Url => TargetUri.OriginalString;

		/// <summary>
		/// Terminates the underlying Tcp Connection to client after current request
		/// </summary>
		internal bool CancelRequest { get; set; }

		/// <summary>
		/// Does this request has an upgrade to websocket header?
		/// </summary>
		internal bool UpgradeToWebSocket
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("upgrade", out header);

				return header?.Value?.Equals("websocket", StringComparison.InvariantCultureIgnoreCase) ?? false;
			}
		}

		/// <summary>
		/// Does server responsed positively for 100 continue request
		/// </summary>
		public bool Is100Continue { get; internal set; }

		/// <summary>
		/// Server responsed negatively for the request for 100 continue
		/// </summary>
		public bool ExpectationFailed { get; internal set; }
	}
}
