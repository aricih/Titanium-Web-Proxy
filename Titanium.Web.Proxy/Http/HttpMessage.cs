using System;
using System.Collections.Generic;
using System.Text;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Http
{
	/// <summary>
	/// Class HttpMessage.
	/// </summary>
	public class HttpMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HttpMessage"/> class.
		/// </summary>
		public HttpMessage()
		{
			Headers = new Dictionary<string, HttpHeader>(StringComparer.OrdinalIgnoreCase);
			NonUniqueHeaders = new Dictionary<string, List<HttpHeader>>(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Gets the content encoding.
		/// </summary>
		internal string ContentEncoding
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("content-encoding", out header);

				return header?.Value;
			}
		}

		/// <summary>
		/// Gets or sets the length of the content.
		/// </summary>
		public long ContentLength
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("content-length", out header);

				if (header == null)
				{
					return -1;
				}

				long contentLen;

				if (long.TryParse(header.Value, out contentLen) && contentLen >= 0)
				{
					return contentLen;
				}

				return -1;
			}
			set
			{
				HttpHeader header;

				Headers.TryGetValue("content-length", out header);

				if (value >= 0)
				{
					if (header != null)
					{
						header.Value = value.ToString();
					}
					else
					{
						Headers.Add("content-length", new HttpHeader("content-length", value.ToString()));
					}

					IsChunked = false;
				}
				else
				{
					if (header != null)
					{
						Headers.Remove("content-length");
					}
				}

			}
		}

		/// <summary>
		/// Gets or sets the type of the content.
		/// </summary>
		public string ContentType
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("content-type", out header);

				return header?.Value;
			}
			set
			{
				HttpHeader header;

				Headers.TryGetValue("content-type", out header);

				if (header != null)
				{
					header.Value = value;
				}
				else
				{
					Headers.Add("content-type", new HttpHeader("content-type", value));
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is chunked.
		/// </summary>
		public bool IsChunked
		{
			get
			{
				HttpHeader header;

				Headers.TryGetValue("transfer-encoding", out header);

				return header?.Value?.ToLower().Contains("chunked") ?? false;
			}
			set
			{
				HttpHeader header;

				Headers.TryGetValue("transfer-encoding", out header);

				if (value)
				{
					if (header != null)
					{
						header.Value = "chunked";
					}
					else
					{
						Headers.Add("transfer-encoding", new HttpHeader("transfer-encoding", "chunked"));
					}

					ContentLength = -1;
				}
				else
				{
					if (header != null)
					{
						Headers.Remove("transfer-encoding");
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the HTTP version.
		/// </summary>
		public Version HttpVersion { get; set; }

		/// <summary>
		/// response body contenst as byte array
		/// </summary>
		internal byte[] Body { get; set; }

		/// <summary>
		/// response body as string
		/// </summary>
		internal string BodyString { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance has body.
		/// </summary>
		internal bool HasBody => ContentLength > 0;

		/// <summary>
		/// Gets or sets a value indicating whether [response body read].
		/// </summary>
		internal bool HasBodyRead { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [response locked].
		/// </summary>
		internal bool Locked { get; set; }

		/// <summary>
		/// Gets the encoding.
		/// </summary>
		internal Encoding Encoding
		{
			get
			{
				try
				{
					// Return default encoding if not specified
					if (ContentType == null)
					{
						return ProxyConstants.DefaultEncoding;
					}

					// Extract the encoding by finding the charset
					var contentTypes = ContentType.Split(ProxyConstants.SemiColonSplit);

					foreach (var contentType in contentTypes)
					{
						var encodingSplit = contentType.Split('=');

						if (encodingSplit.Length == 2 && encodingSplit[0].Trim().Equals("charset", StringComparison.InvariantCultureIgnoreCase))
						{
							return Encoding.GetEncoding(encodingSplit[1]);
						}
					}
				}
				catch
				{
					// Ignore parsing errors
				}

				return ProxyConstants.DefaultEncoding;
			}
		}

		/// <summary>
		/// Collection of all headers
		/// </summary>
		public Dictionary<string, HttpHeader> Headers { get; set; }

		/// <summary>
		/// Non Unique headers
		/// </summary>
		public Dictionary<string, List<HttpHeader>> NonUniqueHeaders { get; set; }
	}
}