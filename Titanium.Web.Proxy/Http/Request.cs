﻿using System;
using System.Collections.Generic;
using System.Text;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Extensions;

namespace Titanium.Web.Proxy.Http
{
	/// <summary>
	/// A HTTP(S) request object
	/// </summary>
	public class Request
	{
		/// <summary>
		/// Request Method
		/// </summary>
		public string Method { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance has body.
		/// </summary>
		internal bool HasBody => ContentLength > 0;

		/// <summary>
		/// Gets or sets a value indicating whether request body will be recorded.
		/// </summary>
		public bool RecordBody { get; set; }

		/// <summary>
		/// Request HTTP Uri
		/// </summary>
		public Uri RequestUri { get; set; }

		/// <summary>
		/// Request Http Version
		/// </summary>
		public Version HttpVersion { get; set; }

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
				var hasHeader = RequestHeaders.ContainsKey("host");

				return hasHeader ? RequestHeaders["host"].Value : null;
			}
			set
			{
				var hasHeader = RequestHeaders.ContainsKey("host");

				if (hasHeader)
				{
					RequestHeaders["host"].Value = value;
				}
				else
				{
					RequestHeaders.Add("Host", new HttpHeader("Host", value));
				}

			}
		}

		/// <summary>
		/// Request content encoding
		/// </summary>
		internal string ContentEncoding
		{
			get
			{
				var hasHeader = RequestHeaders.ContainsKey("content-encoding");

				return hasHeader ? RequestHeaders["content-encoding"].Value : null;
			}
		}

		/// <summary>
		/// Request content-length
		/// </summary>
		public long ContentLength
		{
			get
			{
				var hasHeader = RequestHeaders.ContainsKey("content-length");

				if (hasHeader == false)
				{
					return -1;
				}

				var header = RequestHeaders["content-length"];

				long contentLen;
				long.TryParse(header.Value, out contentLen);
				if (contentLen >= 0)
				{
					return contentLen;
				}

				return -1;
			}
			set
			{
				var hasHeader = RequestHeaders.ContainsKey("content-length");

				if (value >= 0)
				{
					if (hasHeader)
					{
						var header = RequestHeaders["content-length"];

						header.Value = value.ToString();
					}
					else
					{
						RequestHeaders.Add("content-length", new HttpHeader("content-length", value.ToString()));
					}

					IsChunked = false;
				}
				else
				{
					if (hasHeader)
					{
						RequestHeaders.Remove("content-length");
					}

				}

			}
		}

		/// <summary>
		/// Request content-type
		/// </summary>
		public string ContentType
		{
			get
			{
				var hasHeader = RequestHeaders.ContainsKey("content-type");

				if (!hasHeader)
				{
					return null;
				}

				var header = RequestHeaders["content-type"];
				return header.Value;
			}
			set
			{
				var hasHeader = RequestHeaders.ContainsKey("content-type");

				if (hasHeader)
				{
					var header = RequestHeaders["content-type"];
					header.Value = value;
				}
				else
				{
					RequestHeaders.Add("content-type", new HttpHeader("content-type", value));
				}
			}

		}

		/// <summary>
		/// Is request body send as chunked bytes
		/// </summary>
		public bool IsChunked
		{
			get
			{
				var hasHeader = RequestHeaders.ContainsKey("transfer-encoding");

				if (!hasHeader)
				{
					return false;
				}

				var header = RequestHeaders["transfer-encoding"];

				return header.Value?.ToLower().Contains("chunked") ?? false;
			}
			set
			{
				var hasHeader = RequestHeaders.ContainsKey("transfer-encoding");

				if (value)
				{
					if (hasHeader)
					{
						var header = RequestHeaders["transfer-encoding"];
						header.Value = "chunked";
					}
					else
					{
						RequestHeaders.Add("transfer-encoding", new HttpHeader("transfer-encoding", "chunked"));
					}

					ContentLength = -1;
				}
				else
				{
					if (hasHeader)
					{
						RequestHeaders.Remove("transfer-encoding");
					}
				}
			}
		}

		/// <summary>
		/// Does this request has a 100-continue header?
		/// </summary>
		public bool ExpectContinue
		{
			get
			{
				var hasHeader = RequestHeaders.ContainsKey("expect");

				if (!hasHeader)
				{
					return false;
				}

				var header = RequestHeaders["expect"];

				return header.Value?.Equals("100-continue", StringComparison.OrdinalIgnoreCase) ?? false;
			}
		}

		/// <summary>
		/// Request Url
		/// </summary>
		public string Url => RequestUri.OriginalString;

		/// <summary>
		/// Encoding for this request
		/// </summary>
		internal Encoding Encoding => this.GetEncoding();

		/// <summary>
		/// Terminates the underlying Tcp Connection to client after current request
		/// </summary>
		internal bool CancelRequest { get; set; }

		/// <summary>
		/// Request body as byte array
		/// </summary>
		internal byte[] RequestBody { get; set; }

		/// <summary>
		/// request body as string
		/// </summary>
		internal string RequestBodyString { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [request body read].
		/// </summary>
		internal bool RequestBodyRead { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [request locked].
		/// </summary>
		internal bool RequestLocked { get; set; }

		/// <summary>
		/// Does this request has an upgrade to websocket header?
		/// </summary>
		internal bool UpgradeToWebSocket
		{
			get
			{
				var hasHeader = RequestHeaders.ContainsKey("upgrade");

				if (hasHeader == false)
				{
					return false;
				}

				var header = RequestHeaders["upgrade"];

				return header.Value.ToLower() == "websocket";
			}
		}

		/// <summary>
		/// Unique Request header collection
		/// </summary>
		public Dictionary<string, HttpHeader> RequestHeaders { get; set; }

		/// <summary>
		/// Non Unique headers
		/// </summary>
		public Dictionary<string, List<HttpHeader>> NonUniqueRequestHeaders { get; set; }

		/// <summary>
		/// Does server responsed positively for 100 continue request
		/// </summary>
		public bool Is100Continue { get; internal set; }

		/// <summary>
		/// Server responsed negatively for the request for 100 continue
		/// </summary>
		public bool ExpectationFailed { get; internal set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Request"/> class.
		/// </summary>
		public Request()
		{
			RequestHeaders = new Dictionary<string, HttpHeader>(StringComparer.OrdinalIgnoreCase);
			NonUniqueRequestHeaders = new Dictionary<string, List<HttpHeader>>(StringComparer.OrdinalIgnoreCase);
		}

	}
}
