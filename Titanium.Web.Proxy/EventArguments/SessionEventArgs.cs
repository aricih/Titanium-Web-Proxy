using System;
using System.Collections.Generic;
using System.IO;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Decompression;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Http.Responses;
using Titanium.Web.Proxy.Extensions;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Network;
using System.Net;
using System.Threading;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.EventArguments
{
	/// <summary>
	/// Holds info related to a single proxy session (single request/response sequence)
	/// A proxy session is bounded to a single connection from client
	/// A proxy session ends when client terminates connection to proxy
	/// or when server terminates connection from proxy
	/// </summary>
	public class SessionEventArgs : EventArgs, IDisposable
	{
		/// <summary>
		/// Size of Buffers used by this object
		/// </summary>
		private readonly int _bufferSize;

		/// <summary>
		/// Holds a reference to proxy response handler method
		/// </summary>
		private readonly Func<SessionEventArgs, CancellationToken, Task> _httpResponseHandler;

		/// <summary>
		/// Holds a reference to client
		/// </summary>
		internal ProxyClient ProxyClient { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether request replay is needed.
		/// </summary>
		public bool ReRequest
		{
			get;
			set;
		}

		/// <summary>
		/// Does this session uses SSL
		/// </summary>
		public bool IsHttps => WebSession.Request.RequestUri.Scheme == Uri.UriSchemeHttps;

		/// <summary>
		/// Gets the client end point.
		/// </summary>
		public IPEndPoint ClientEndPoint => (IPEndPoint)ProxyClient.TcpClient.Client.RemoteEndPoint;

		/// <summary>
		/// A web session corresponding to a single request/response sequence
		/// within a proxy connection
		/// </summary>
		public HttpWebClient WebSession { get; set; }

		/// <summary>
		/// Gets or sets the custom up stream HTTP proxy used.
		/// </summary>
		public ExternalProxy CustomUpStreamHttpProxyUsed { get; set; }

		/// <summary>
		/// Gets or sets the custom up stream HTTPS proxy used.
		/// </summary>
		public ExternalProxy CustomUpStreamHttpsProxyUsed { get; set; }

		/// <summary>
		/// Gets or sets the last status code.
		/// </summary>
		internal HttpStatusCode LastStatusCode { get; set; }

		/// <summary>
		/// Constructor to initialize the proxy
		/// </summary>
		internal SessionEventArgs(int bufferSize, Func<SessionEventArgs, CancellationToken, Task> httpResponseHandler)
		{
			_bufferSize = bufferSize;
			_httpResponseHandler = httpResponseHandler;

			ProxyClient = new ProxyClient();
			WebSession = new HttpWebClient();
		}

		/// <summary>
		/// Read request body content as bytes[] for current session
		/// </summary>
		private async Task ReadRequestBody(CancellationToken cancellationToken = default(CancellationToken))
		{
			//GET request don't have a request body to read
			if (!WebSession.Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase) 
				&& !WebSession.Request.Method.Equals("PUT", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new BodyNotFoundException("Request don't have a body." +
												"Please verify that this request is a Http POST/PUT and request " +
												"content length is greater than zero before accessing the body.");
			}

			//Caching check
			if (WebSession.Request.RequestBody == null)
			{

				//If chunked then its easy just read the whole body with the content length mentioned in the request header
				using (var requestBodyStream = new MemoryStream())
				{
					//For chunked request we need to read data as they arrive, until we reach a chunk end symbol
					if (WebSession.Request.IsChunked)
					{
						await ProxyClient.ClientStreamReader.CopyBytesToStreamChunked(_bufferSize, requestBodyStream, cancellationToken: cancellationToken);
					}
					else
					{
						//If not chunked then its easy just read the whole body with the content length mentioned in the request header
						if (WebSession.Request.ContentLength > 0)
						{
							//If not chunked then its easy just read the amount of bytes mentioned in content length header of response
							await ProxyClient.ClientStreamReader.CopyBytesToStream(
								_bufferSize,
								requestBodyStream,
								WebSession.Request.ContentLength,
								cancellationToken: cancellationToken);

						}
						else if (WebSession.Request.HttpVersion.Major == 1 && WebSession.Request.HttpVersion.Minor == 0)
						{
							await WebSession.ServerConnection.StreamReader.CopyBytesToStream(
								_bufferSize,
								requestBodyStream,
								long.MaxValue,
								cancellationToken: cancellationToken);
						}
					}
					WebSession.Request.RequestBody = await GetDecompressedResponseBody(
						WebSession.Request.ContentEncoding,
						requestBodyStream.ToArray(),
						cancellationToken: cancellationToken);
				}

				//Now set the flag to true
				//So that next time we can deliver body from cache
				WebSession.Request.RequestBodyRead = true;
			}

		}

		/// <summary>
		/// Read response body as byte[] for current response
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		private async Task ReadResponseBody(CancellationToken cancellationToken = default(CancellationToken))
		{
			//If not already read (not cached yet)
			if (WebSession.Response.ResponseBody == null)
			{
				using (var responseBodyStream = new MemoryStream())
				{
					//If chuncked the read chunk by chunk until we hit chunk end symbol
					if (WebSession.Response.IsChunked)
					{
						await WebSession.ServerConnection.StreamReader.CopyBytesToStreamChunked(_bufferSize, responseBodyStream, cancellationToken: cancellationToken);
					}
					else
					{
						if (WebSession.Response.ContentLength > 0)
						{
							//If not chunked then its easy just read the amount of bytes mentioned in content length header of response
							await WebSession.ServerConnection.StreamReader.CopyBytesToStream(
								_bufferSize,
								responseBodyStream,
								WebSession.Response.ContentLength,
								cancellationToken: cancellationToken);

						}
						else if ((WebSession.Response.HttpVersion.Major == 1 && WebSession.Response.HttpVersion.Minor == 0) || WebSession.Response.ContentLength == -1)
						{
							await WebSession.ServerConnection.StreamReader.CopyBytesToStream(
								_bufferSize,
								responseBodyStream,
								long.MaxValue,
								cancellationToken: cancellationToken);
						}
					}

					WebSession.Response.ResponseBody = await GetDecompressedResponseBody(
						WebSession.Response.ContentEncoding,
						responseBodyStream.ToArray(),
						cancellationToken: cancellationToken);

				}
				//set this to true for caching
				WebSession.Response.ResponseBodyRead = true;
			}
		}

		/// <summary>
		/// Gets the request body as bytes
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Request body as byte array.</returns>
		/// <exception cref="Exception">You cannot call this function after request is made to server.</exception>
		public async Task<byte[]> GetRequestBody(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!WebSession.Request.RequestBodyRead)
			{
				if (WebSession.Request.RequestLocked)
				{
					throw new Exception("You cannot call this function after request is made to server.");
				}

				await ReadRequestBody(cancellationToken: cancellationToken);
			}
			return WebSession.Request.RequestBody;
		}

		/// <summary>
		/// Gets the request body as string
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Request body as string.</returns>
		public async Task<string> GetRequestBodyAsString(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!WebSession.Request.RequestBodyRead)
			{
				if (WebSession.Request.RequestLocked)
				{
					string requestBody;

					return ProxyServer.Instance.RequestBodyCache.Value.TryRemove(WebSession.RequestId, out requestBody)
						? requestBody
						: null;
				}

				await ReadRequestBody(cancellationToken: cancellationToken);
			}
			// Use the encoding specified in request to decode the byte[] data to string
			return WebSession.Request.RequestBodyString ?? (WebSession.Request.RequestBodyString = WebSession.Request.Encoding.GetString(WebSession.Request.RequestBody));
		}

		/// <summary>
		/// Sets the request body
		/// </summary>
		/// <param name="body">The body.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="Exception">You cannot call this function after request is made to server.</exception>
		public async Task SetRequestBody(byte[] body, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function after request is made to server.");
			}

			//syphon out the request body from client before setting the new body
			if (!WebSession.Request.RequestBodyRead)
			{
				await ReadRequestBody(cancellationToken: cancellationToken);
			}

			WebSession.Request.RequestBody = body;

			if (WebSession.Request.IsChunked == false)
			{
				WebSession.Request.ContentLength = body.Length;
			}
			else
			{
				WebSession.Request.ContentLength = -1;
			}
		}

		/// <summary>
		/// Sets the body with the specified string
		/// </summary>
		/// <param name="body">The body.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="Exception">You cannot call this function after request is made to server.</exception>
		public async Task SetRequestBodyString(string body, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function after request is made to server.");
			}

			//syphon out the request body from client before setting the new body
			if (!WebSession.Request.RequestBodyRead)
			{
				await ReadRequestBody(cancellationToken: cancellationToken);
			}

			await SetRequestBody(WebSession.Request.Encoding.GetBytes(body), cancellationToken: cancellationToken);

		}

		/// <summary>
		/// Gets the response body as byte array
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Task&lt;System.Byte[]&gt;.</returns>
		/// <exception cref="Exception">You cannot call this function before request is made to server.</exception>
		public async Task<byte[]> GetResponseBody(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function before request is made to server.");
			}

			await ReadResponseBody(cancellationToken: cancellationToken);
			return WebSession.Response.ResponseBody;
		}

		/// <summary>
		/// Gets the response body as string
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Response body as string.</returns>
		/// <exception cref="Exception">You cannot call this function before request is made to server.</exception>
		public async Task<string> GetResponseBodyAsString(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function before request is made to server.");
			}

			await GetResponseBody(cancellationToken: cancellationToken);

			return WebSession.Response.ResponseBodyString ??
				(WebSession.Response.ResponseBodyString = WebSession.Response.Encoding.GetString(WebSession.Response.ResponseBody));
		}

		/// <summary>
		/// Set the response body bytes
		/// </summary>
		/// <param name="body">The body.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="Exception">You cannot call this function before request is made to server.</exception>
		public async Task SetResponseBody(byte[] body, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function before request is made to server.");
			}

			//syphon out the response body from server before setting the new body
			if (WebSession.Response.ResponseBody == null)
			{
				await GetResponseBody(cancellationToken: cancellationToken);
			}

			WebSession.Response.ResponseBody = body;

			//If there is a content length header update it
			if (WebSession.Response.IsChunked == false)
			{
				WebSession.Response.ContentLength = body.Length;
			}
			else
			{
				WebSession.Response.ContentLength = -1;
			}
		}

		/// <summary>
		/// Replace the response body with the specified string
		/// </summary>
		/// <param name="body">The body.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="Exception">You cannot call this function before request is made to server.</exception>
		public async Task SetResponseBodyString(string body, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function before request is made to server.");
			}

			//syphon out the response body from server before setting the new body
			if (WebSession.Response.ResponseBody == null)
			{
				await GetResponseBody(cancellationToken: cancellationToken);
			}

			var bodyBytes = WebSession.Response.Encoding.GetBytes(body);

			await SetResponseBody(bodyBytes, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Gets the decompressed response body.
		/// </summary>
		/// <param name="encodingType">Type of the encoding.</param>
		/// <param name="responseBodyStream">The response body stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Decompressed response body.</returns>
		private async Task<byte[]> GetDecompressedResponseBody(string encodingType, byte[] responseBodyStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			var decompressionFactory = new DecompressionFactory();
			var decompressor = decompressionFactory.Create(encodingType);

			return await decompressor.Decompress(responseBodyStream, _bufferSize, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Before request is made to server
		/// Respond with the specified HTML string to client
		/// and ignore the request
		/// </summary>
		/// <param name="html">The HTML.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public async Task Ok(string html, CancellationToken cancellationToken = default(CancellationToken))
		{
			await Ok(html, null, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Before request is made to server
		/// Respond with the specified HTML string to client
		/// and ignore the request
		/// </summary>
		/// <param name="html">The HTML.</param>
		/// <param name="headers">The headers.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="Exception">You cannot call this function after request is made to server.</exception>
		public async Task Ok(string html, Dictionary<string, HttpHeader> headers, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (WebSession.Request.RequestLocked)
			{
				throw new Exception("You cannot call this function after request is made to server.");
			}

			if (html == null)
			{
				html = string.Empty;
			}

			var result = ProxyConstants.DefaultEncoding.GetBytes(html);

			await Ok(result, headers, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Before request is made to server
		/// Respond with the specified byte[] to client
		/// and ignore the request
		/// </summary>
		/// <param name="result">The result.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public async Task Ok(byte[] result, CancellationToken cancellationToken = default(CancellationToken))
		{
			await Ok(result, null, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Before request is made to server
		/// Respond with the specified byte[] to client
		/// and ignore the request
		/// </summary>
		/// <param name="result">The result.</param>
		/// <param name="headers">The headers.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public async Task Ok(byte[] result, Dictionary<string, HttpHeader> headers, CancellationToken cancellationToken = default(CancellationToken))
		{
			var response = new OkResponse();
			if (headers != null && headers.Count > 0)
			{
				response.ResponseHeaders = headers;
			}
			response.HttpVersion = WebSession.Request.HttpVersion;
			response.ResponseBody = result;

			await Respond(response, cancellationToken: cancellationToken);

			WebSession.Request.CancelRequest = true;
		}

		/// <summary>
		/// Redirects the specified URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public async Task Redirect(string url, CancellationToken cancellationToken = default(CancellationToken))
		{
			var response = new RedirectResponse { HttpVersion = WebSession.Request.HttpVersion };

			response.ResponseHeaders.Add("Location", new HttpHeader("Location", url));
			response.ResponseBody = ProxyConstants.DefaultEncoding.GetBytes(string.Empty);

			await Respond(response, cancellationToken: cancellationToken);

			WebSession.Request.CancelRequest = true;
		}

		/// <summary>
		/// Generic responder.
		/// </summary>
		/// <param name="response">The response.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public async Task Respond(Response response, CancellationToken cancellationToken = default(CancellationToken))
		{
			WebSession.Request.RequestLocked = true;

			response.ResponseLocked = true;
			response.ResponseBodyRead = true;

			WebSession.Response = response;

			await _httpResponseHandler(this, cancellationToken);
		}

		/// <summary>
		/// implement any cleanup here
		/// </summary>
		public void Dispose()
		{
		}
	}
}