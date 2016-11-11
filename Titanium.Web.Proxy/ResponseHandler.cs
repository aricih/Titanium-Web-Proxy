﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Compression;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Authentication;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Extensions;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy
{
	/// <summary>
	/// Handle the response from server
	/// </summary>
	partial class ProxyServer
	{
		public async Task<bool> HandleAuthenticationRequired(SessionEventArgs args)
		{
			var isRequestReplayNeeded = false;
			HttpStatusCode httpStatusCode;

			// Status code could not be parsed, no need to replay the request
			if (!Enum.TryParse(args.WebSession.Response.ResponseStatusCode, out httpStatusCode))
			{
				return isRequestReplayNeeded;
			}

			isRequestReplayNeeded = httpStatusCode == HttpStatusCode.Unauthorized
				|| httpStatusCode == HttpStatusCode.ProxyAuthenticationRequired;

			var credentialHeader =
				args.WebSession.Response.ResponseHeaders.Values.FirstOrDefault(
					header => header.Name.EndsWith("-Authenticate", StringComparison.CurrentCultureIgnoreCase));

			// Request must be authenticated
			if (isRequestReplayNeeded)
			{
				var isProxified = (UpStreamHttpProxy != null && args.WebSession.Request.Host != UpStreamHttpProxy.HostName)
					|| (UpStreamHttpsProxy != null && args.WebSession.Request.Host != UpStreamHttpsProxy.HostName);

				var upstreamProxy = isProxified ? (args.IsHttps ? UpStreamHttpsProxy : UpStreamHttpProxy) : null;

				var credentialProvider = (isProxified && httpStatusCode != HttpStatusCode.Unauthorized)
					? (ICredentialProvider) new ProxyCredentialProvider(upstreamProxy)
					: new HttpCredentialProvider(args.WebSession.Request.RequestUri.Host);

				// If we're stuck at same http status code instead of preauthentication
				// then preauthentication is failing most likely because of an expired authorization token in cache
				// so set preauthentication failed flag to invalidate corresponding cache entry
				var preAuthenticationFailed = args.WebSession.ServerConnection.PreAuthenticateUsed
					&& args.LastStatusCode == httpStatusCode;

				await AuthenticationClient.Authenticate(
					args.WebSession.Request.RequestUri,
					credentialHeader,
					credentialProvider,
					args.WebSession.Request.RequestHeaders,
					preAuthenticationFailed,
					(args.WebSession.ServerConnection.Stream as SslStream)?.TransportContext);
			}

			// Update last status code on session
			args.LastStatusCode = httpStatusCode;

			return isRequestReplayNeeded;
		}

		//Called asynchronously when a request was successfully and we received the response
		public async Task HandleHttpSessionResponse(SessionEventArgs args)
		{
			try
			{
				// Read response & headers from server
				await args.WebSession.ReceiveResponse(args.ReRequest);

				if (!args.WebSession.Response.ResponseBodyRead)
				{
					args.WebSession.Response.ResponseStream = args.WebSession.ServerConnection.Stream;
				}

				args.ReRequest = await HandleAuthenticationRequired(args);

				//If user requested call back then do it
				if (BeforeResponse != null && !args.WebSession.Response.ResponseLocked)
				{
					Delegate[] invocationList = BeforeResponse.GetInvocationList();
					Task[] handlerTasks = new Task[invocationList.Length];

					for (int i = 0; i < invocationList.Length; i++)
					{
						handlerTasks[i] = ((Func<object, SessionEventArgs, Task>)invocationList[i])(this, args);
					}

					await Task.WhenAll(handlerTasks);
				}

				if (args.ReRequest)
				{
					await HandleHttpSessionRequestInternal(null, args, UpStreamHttpProxy, UpStreamHttpsProxy, true).ConfigureAwait(false);
					return;
				}

				args.WebSession.Response.ResponseLocked = true;

				//Write back to client 100-conitinue response if that's what server returned
				if (args.WebSession.Response.Is100Continue)
				{
					await WriteResponseStatus(args.WebSession.Response.HttpVersion, "100",
							"Continue", args.ProxyClient.ClientStreamWriter);
					await args.ProxyClient.ClientStreamWriter.WriteLineAsync();
				}
				else if (args.WebSession.Response.ExpectationFailed)
				{
					await WriteResponseStatus(args.WebSession.Response.HttpVersion, "417",
							"Expectation Failed", args.ProxyClient.ClientStreamWriter);
					await args.ProxyClient.ClientStreamWriter.WriteLineAsync();
				}

				//Write back response status to client
				await WriteResponseStatus(args.WebSession.Response.HttpVersion, args.WebSession.Response.ResponseStatusCode,
							  args.WebSession.Response.ResponseStatusDescription, args.ProxyClient.ClientStreamWriter);

				if (args.WebSession.Response.ResponseBodyRead)
				{
					var isChunked = args.WebSession.Response.IsChunked;
					var contentEncoding = args.WebSession.Response.ContentEncoding;

					if (contentEncoding != null)
					{
						args.WebSession.Response.ResponseBody = await GetCompressedResponseBody(contentEncoding, args.WebSession.Response.ResponseBody);

						if (isChunked == false)
						{
							args.WebSession.Response.ContentLength = args.WebSession.Response.ResponseBody.Length;
						}
						else
						{
							args.WebSession.Response.ContentLength = -1;
						}
					}

					await WriteResponseHeaders(args.ProxyClient.ClientStreamWriter, args.WebSession.Response);
					await args.ProxyClient.ClientStream.WriteResponseBody(args.WebSession.Response.ResponseBody, isChunked);
				}
				else
				{
					await WriteResponseHeaders(args.ProxyClient.ClientStreamWriter, args.WebSession.Response);

					//Write body only if response is chunked or content length >0
					//Is none are true then check if connection:close header exist, if so write response until server or client terminates the connection
					if (args.WebSession.Response.IsChunked || args.WebSession.Response.ContentLength > 0
						|| !args.WebSession.Response.ResponseKeepAlive)
					{
						await args.WebSession.ServerConnection.StreamReader
							.WriteResponseBody(BUFFER_SIZE, args.ProxyClient.ClientStream, args.WebSession.Response.IsChunked,
							args.WebSession.Response.ContentLength);
					}
					//write response if connection:keep-alive header exist and when version is http/1.0
					//Because in Http 1.0 server can return a response without content-length (expectation being client would read until end of stream)
					else if (args.WebSession.Response.ResponseKeepAlive && args.WebSession.Response.HttpVersion.Minor == 0)
					{
						await args.WebSession.ServerConnection.StreamReader
							.WriteResponseBody(BUFFER_SIZE, args.ProxyClient.ClientStream, args.WebSession.Response.IsChunked,
							args.WebSession.Response.ContentLength);
					}
				}

				await args.ProxyClient.ClientStream.FlushAsync();

			}
			catch (Exception e)
			{
				ExceptionFunc(new ProxyHttpException("Error occured wilst handling session response", e, args));
				Dispose(args.ProxyClient.ClientStream, args.ProxyClient.ClientStreamReader,
					args.ProxyClient.ClientStreamWriter, args);
				throw;
			}
			finally
			{
				// Remove cache entry if request body is recorded
				if (args.WebSession.Request.HasBody && args.WebSession.Request.RecordBody)
				{
					string recordedBody;
					RequestBodyCache.Value.TryRemove(args.WebSession.RequestId, out recordedBody);
				}

				args.Dispose();
			}
		}

		/// <summary>
		/// get the compressed response body from give response bytes
		/// </summary>
		/// <param name="encodingType"></param>
		/// <param name="responseBodyStream"></param>
		/// <returns></returns>
		private async Task<byte[]> GetCompressedResponseBody(string encodingType, byte[] responseBodyStream)
		{
			var compressionFactory = new CompressionFactory();
			var compressor = compressionFactory.Create(encodingType);
			return await compressor.Compress(responseBodyStream);
		}

		/// <summary>
		/// Write response status
		/// </summary>
		/// <param name="version"></param>
		/// <param name="code"></param>
		/// <param name="description"></param>
		/// <param name="responseWriter"></param>
		/// <returns></returns>
		private async Task WriteResponseStatus(Version version, string code, string description,
			StreamWriter responseWriter)
		{
			await responseWriter.WriteLineAsync($"HTTP/{version.Major}.{version.Minor} {code} {description}");
		}

		/// <summary>
		/// Write response headers to client
		/// </summary>
		/// <param name="responseWriter"></param>
		/// <param name="headers"></param>
		/// <returns></returns>
		private async Task WriteResponseHeaders(StreamWriter responseWriter, Response response)
		{
			FixProxyHeaders(response.ResponseHeaders);

			foreach (var header in response.ResponseHeaders)
			{
				await responseWriter.WriteLineAsync(header.Value.ToString());
			}

			//write non unique request headers
			foreach (var headerItem in response.NonUniqueResponseHeaders)
			{
				var headers = headerItem.Value;
				foreach (var header in headers)
				{
					await responseWriter.WriteLineAsync(header.ToString());
				}
			}


			await responseWriter.WriteLineAsync();
			await responseWriter.FlushAsync();
		}

		/// <summary>
		/// Fix proxy specific headers
		/// </summary>
		/// <param name="headers"></param>
		private void FixProxyHeaders(Dictionary<string, HttpHeader> headers)
		{
			//If proxy-connection close was returned inform to close the connection
			var hasProxyHeader = headers.ContainsKey("proxy-connection");
			var hasConnectionheader = headers.ContainsKey("connection");

			if (hasProxyHeader)
			{
				var proxyHeader = headers["proxy-connection"];
				if (hasConnectionheader == false)
				{
					headers.Add("connection", new HttpHeader("connection", proxyHeader.Value));
				}
				else
				{
					var connectionHeader = headers["connection"];
					connectionHeader.Value = proxyHeader.Value;
				}

				headers.Remove("proxy-connection");
			}

		}

		/// <summary>
		/// Handle dispose of a client/server session
		/// </summary>
		/// <param name="tcpClient"></param>
		/// <param name="clientStream"></param>
		/// <param name="clientStreamReader"></param>
		/// <param name="clientStreamWriter"></param>
		/// <param name="args"></param>
		private void Dispose(Stream clientStream, CustomBinaryReader clientStreamReader,
			StreamWriter clientStreamWriter, IDisposable args)
		{

			if (clientStream != null)
			{
				clientStream.Close();
				clientStream.Dispose();
			}

			if (args != null)
			{
				args.Dispose();
			}

			if (clientStreamReader != null)
			{
				clientStreamReader.Dispose();
			}

			if (clientStreamWriter != null)
			{
				clientStreamWriter.Close();
				clientStreamWriter.Dispose();
			}
		}
	}
}