using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Extensions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy
{
	/// <summary>
	/// Handle the request
	/// </summary>
	public partial class ProxyServer
	{
		internal readonly ConcurrentDictionary<Guid, string> RequestBodyCache = new ConcurrentDictionary<Guid, string>();

		//This is called when client is aware of proxy
		//So for HTTPS requests client would send CONNECT header to negotiate a secure tcp tunnel via proxy
		private async Task HandleClient(ExplicitProxyEndpoint endpoint, TcpClient tcpClient, CancellationToken cancellationToken = default(CancellationToken))
		{
			var requestBeginTimestamp = DateTime.UtcNow;

			Stream clientStream = tcpClient.GetStream();

			clientStream.ReadTimeout = ConnectionTimeOutSeconds * 1000;
			clientStream.WriteTimeout = ConnectionTimeOutSeconds * 1000;

			var clientStreamReader = new CustomBinaryReader(clientStream);
			var clientStreamWriter = new StreamWriter(clientStream, Encoding.ASCII, BufferSize, true);

			try
			{
				//read the first line HTTP command
				var httpCmd = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);

				if (string.IsNullOrEmpty(httpCmd))
				{
					Dispose(clientStream, clientStreamReader, clientStreamWriter, null);
					return;
				}

				// Break up the line into three components (method, remote URL & Http Version)
				var httpRequestHead = HttpRequestHeadParser.Parse(httpCmd);

				var httpRemoteUri = httpRequestHead.Method.Equals("CONNECT", StringComparison.InvariantCultureIgnoreCase)
					? new Uri($"http://{httpRequestHead.Url}")
					: new Uri(httpRequestHead.Url);

				// Filter out excluded host names
				var excluded = endpoint.ExcludedHttpsHostNameRegex?.Any(httpRemoteUri.Host.Contains) ?? false;

				List<HttpHeader> connectRequestHeaders = null;

				//Client wants to create a secure tcp tunnel (its a HTTPS request)
				if (httpRequestHead.Method.Equals("CONNECT", StringComparison.InvariantCultureIgnoreCase) && !excluded && httpRemoteUri.Port != 80)
				{
					httpRemoteUri = new Uri($"https://{httpRequestHead.Url}");

					string line;
					connectRequestHeaders = new List<HttpHeader>();

					while (!string.IsNullOrEmpty(line = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken)))
					{
						var header = line.Split(ProxyConstants.ColonSplit, 2);

						var newHeader = new HttpHeader(header[0], header[1]);
						connectRequestHeaders.Add(newHeader);
					}

					if (await CheckAuthorization(clientStreamWriter, connectRequestHeaders) == false)
					{
						Dispose(clientStream, clientStreamReader, clientStreamWriter, null);
						return;
					}

					await WriteConnectResponse(clientStreamWriter, httpRequestHead.Version);

					SslStream sslStream = null;

					try
					{
						sslStream = new SslStream(clientStream, true);

						var certificate = _certificateCacheManager.GetOrCreateCertificate(httpRemoteUri.Host, false);

						// Successfully managed to authenticate the client using the fake certificate
						await sslStream.AuthenticateAsServerAsync(certificate, false, SupportedSslProtocols, false);

						// HTTPS server created - we can now decrypt the client's traffic
						clientStream = sslStream;

						clientStreamReader = new CustomBinaryReader(sslStream);
						clientStreamWriter = new StreamWriter(sslStream);

					}
					catch
					{
						sslStream?.Dispose();

						Dispose(clientStream, clientStreamReader, clientStreamWriter, null);
						return;
					}

					// Now read the actual HTTPS request line
					httpCmd = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);
				}
				else if (httpRequestHead.Method.Equals("CONNECT", StringComparison.InvariantCultureIgnoreCase))
				{
					// Get rid of CONNECT request headers
					await clientStreamReader.ReadAllLinesAsync(cancellationToken: cancellationToken);

					// Write back successful CONNECT response
					await WriteConnectResponse(clientStreamWriter, httpRequestHead.Version);

					await TcpHelper.SendRaw(BufferSize, ConnectionTimeOutSeconds, httpRemoteUri,
							httpCmd, httpRequestHead.Version, null,
							false, SupportedSslProtocols,
							ValidateServerCertificate,
							SelectClientCertificate,
							clientStream, _tcpConnectionFactory,
							cancellationToken: cancellationToken);

					Dispose(clientStream, clientStreamReader, clientStreamWriter, null);
					return;
				}

				if (excluded)
				{
					return;
				}

				// Create the request
				await HandleHttpSessionRequest(tcpClient, httpCmd, clientStream, clientStreamReader, clientStreamWriter,
					  httpRemoteUri.Scheme == Uri.UriSchemeHttps ? httpRemoteUri.Host : null, endpoint, connectRequestHeaders, requestBeginTimestamp, cancellationToken: cancellationToken);
			}
			catch (Exception)
			{
				Dispose(clientStream, clientStreamReader, clientStreamWriter, null);
			}
		}

		//This is called when this proxy acts as a reverse proxy (like a real http server)
		//So for HTTPS requests we would start SSL negotiation right away without expecting a CONNECT request from client
		private async Task HandleClient(TransparentProxyEndpoint endpoint, TcpClient tcpClient, CancellationToken cancellationToken = default(CancellationToken))
		{
			var requestBeginTimestamp = DateTime.UtcNow;

			Stream clientStream = tcpClient.GetStream();

			clientStream.ReadTimeout = ConnectionTimeOutSeconds * 1000;
			clientStream.WriteTimeout = ConnectionTimeOutSeconds * 1000;

			CustomBinaryReader clientStreamReader = null;
			StreamWriter clientStreamWriter = null;

			if (endpoint.EnableSsl)
			{
				var sslStream = new SslStream(clientStream, true);

				//implement in future once SNI supported by SSL stream, for now use the same certificate
				var certificate = _certificateCacheManager.GetOrCreateCertificate(endpoint.GenericCertificateName, false);

				try
				{
					// Successfully managed to authenticate the client using the fake certificate
					await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls, false);

					clientStreamReader = new CustomBinaryReader(sslStream);
					clientStreamWriter = new StreamWriter(sslStream);
					// HTTPS server created - we can now decrypt the client's traffic

				}
				catch (Exception)
				{
					sslStream.Dispose();

					Dispose(sslStream, clientStreamReader, clientStreamWriter, null);
					return;
				}
				clientStream = sslStream;
			}
			else
			{
				clientStreamReader = new CustomBinaryReader(clientStream);
				clientStreamWriter = new StreamWriter(clientStream);
			}

			// Read the request line
			var httpCmd = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);

			// Create the request
			await HandleHttpSessionRequest(tcpClient, httpCmd, clientStream, clientStreamReader, clientStreamWriter,
				 endpoint.EnableSsl ? endpoint.GenericCertificateName : null, endpoint, null, requestBeginTimestamp, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Handles the HTTP session request internal.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="args">The <see cref="SessionEventArgs" /> instance containing the event data.</param>
		/// <param name="customUpStreamHttpProxy">The custom up stream HTTP proxy.</param>
		/// <param name="customUpStreamHttpsProxy">The custom up stream HTTPS proxy.</param>
		/// <param name="closeConnection">if set to <c>true</c> [close connection].</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Task.</returns>
		private async Task HandleHttpSessionRequestInternal(TcpConnection connection, SessionEventArgs args, ExternalProxy customUpStreamHttpProxy,
			ExternalProxy customUpStreamHttpsProxy, bool closeConnection, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				var request = args.WebSession.Request;

				if (connection == null)
				{
					if (request.TargetUri.Scheme == "http")
					{
						if (GetCustomUpStreamHttpProxyFunc != null)
						{
							customUpStreamHttpProxy = await GetCustomUpStreamHttpProxyFunc(args).ConfigureAwait(false);
						}
					}
					else
					{
						if (GetCustomUpStreamHttpsProxyFunc != null)
						{
							customUpStreamHttpsProxy = await GetCustomUpStreamHttpsProxyFunc(args).ConfigureAwait(false);
						}
					}

					args.CustomUpStreamHttpProxyUsed = customUpStreamHttpProxy;
					args.CustomUpStreamHttpsProxyUsed = customUpStreamHttpsProxy;

					connection = await _tcpConnectionFactory.CreateClient(BufferSize, ConnectionTimeOutSeconds,
						request.TargetUri, request.Headers, request.HttpVersion,
						args.IsHttps, SupportedSslProtocols,
						ValidateServerCertificate,
						SelectClientCertificate,
						customUpStreamHttpProxy ?? UpStreamHttpProxy, customUpStreamHttpsProxy ?? UpStreamHttpsProxy,
						args.ProxyClient.ClientStream,
						cancellationToken: cancellationToken);
				}

				if (request.HasBody && request.RecordBody)
				{
					RequestBodyCache[args.WebSession.RequestId] = await args.GetRequestBodyAsString(cancellationToken: cancellationToken);
				}

				request.Locked = true;

				request.CancelRequest = cancellationToken.IsCancellationRequested;

				// If request was cancelled by user then dispose the client
				if (request.CancelRequest)
				{
					Dispose(args.ProxyClient.ClientStream, args.ProxyClient.ClientStreamReader, args.ProxyClient.ClientStreamWriter, args);
					return;
				}

				// If expect continue is enabled then send the headers first 
				// and see if server would return 100 conitinue
				if (request.ExpectContinue)
				{
					args.WebSession.SetConnection(connection);
					await args.WebSession.SendRequest(Enable100ContinueBehaviour, cancellationToken: cancellationToken);
				}

				// If 100 continue was the response inform that to the client
				if (Enable100ContinueBehaviour)
				{
					if (request.Is100Continue)
					{
						await WriteResponseStatus(args.WebSession.Response.HttpVersion, 100,
							"Continue", args.ProxyClient.ClientStreamWriter);
						await args.ProxyClient.ClientStreamWriter.WriteLineAsync();
					}
					else if (request.ExpectationFailed)
					{
						await WriteResponseStatus(args.WebSession.Response.HttpVersion, 417,
							"Expectation Failed", args.ProxyClient.ClientStreamWriter);
						await args.ProxyClient.ClientStreamWriter.WriteLineAsync();
					}
				}

				// If expect continue is not enabled then set the connectio and send request headers
				if (!request.ExpectContinue)
				{
					args.WebSession.SetConnection(connection);
					await args.WebSession.SendRequest(Enable100ContinueBehaviour, cancellationToken: cancellationToken);
				}

				// If request was modified by user
				if (request.HasBodyRead)
				{
					if (request.ContentEncoding != null)
					{
						using (var compressedStream = await GetCompressedResponseBody(
							request.ContentEncoding,
							request.Body,
							cancellationToken: cancellationToken))
						{
							request.Body = compressedStream?.ToArray();
						}
					}

					// Chunked send is not supported as of now
					request.ContentLength = request.Body?.Length ?? 0;

					var newStream = args.WebSession.ServerConnection.Stream;
					await newStream.WriteAsync(request.Body, 0, request.Body?.Length ?? 0, cancellationToken: cancellationToken);
				}
				else
				{
					if (!request.ExpectationFailed)
					{
						// If its a post/put request, then read the client html body and send it to server
						if (request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase)
							|| request.Method.Equals("PUT", StringComparison.InvariantCultureIgnoreCase))
						{
							await SendClientRequestBody(args, cancellationToken: cancellationToken);
						}
					}
				}

				// If not expectation failed response was returned by server then parse response
				if (!request.ExpectationFailed)
				{
					await HandleHttpSessionResponse(args, cancellationToken: cancellationToken);
				}

				// If connection is closing exit
				if (!args.WebSession.Response.KeepAlive)
				{
					Dispose(args.ProxyClient.ClientStream, args.ProxyClient.ClientStreamReader, args.ProxyClient.ClientStreamWriter,
						args);
					return;
				}
			}
			catch (Exception e)
			{
				ExceptionFunc(new ProxyHttpException("Error occured whilst handling session request (internal)", e, args));
				Dispose(args.ProxyClient.ClientStream, args.ProxyClient.ClientStreamReader, args.ProxyClient.ClientStreamWriter,
					args);
				return;
			}

			if (closeConnection)
			{
				connection?.Dispose();
			}
		}

		/// <summary>
		/// This is the core request handler method for a particular connection from client
		/// </summary>
		/// <param name="client">The client.</param>
		/// <param name="httpCmd">The HTTP command.</param>
		/// <param name="clientStream">The client stream.</param>
		/// <param name="clientStreamReader">The client stream reader.</param>
		/// <param name="clientStreamWriter">The client stream writer.</param>
		/// <param name="httpsHostName">Name of the HTTPS host.</param>
		/// <param name="endpoint">The end point.</param>
		/// <param name="connectHeaders">The connect headers.</param>
		/// <param name="requestBeginTimestamp">The request begin timestamp.</param>
		/// <param name="customUpStreamHttpProxy">The custom up stream HTTP proxy.</param>
		/// <param name="customUpStreamHttpsProxy">The custom up stream HTTPS proxy.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private async Task HandleHttpSessionRequest(TcpClient client, string httpCmd, Stream clientStream,
			CustomBinaryReader clientStreamReader, StreamWriter clientStreamWriter, string httpsHostName,
			ProxyEndpoint endpoint, List<HttpHeader> connectHeaders, DateTime requestBeginTimestamp,
			ExternalProxy customUpStreamHttpProxy = null, ExternalProxy customUpStreamHttpsProxy = null,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			TcpConnection connection = null;

			//Loop through each subsequest request on this particular client connection
			//(assuming HTTP connection is kept alive by client)
			while (true)
			{
				if (string.IsNullOrEmpty(httpCmd))
				{
					Dispose(clientStream, clientStreamReader, clientStreamWriter, null);
					break;
				}

				var args = new SessionEventArgs(BufferSize, HandleHttpSessionResponse)
				{
					ProxyClient = { TcpClient = client },
					WebSession = { ConnectHeaders = connectHeaders }
				};

				var request = args.WebSession.Request;

				request.RequestBegin = requestBeginTimestamp;

				args.WebSession.ProcessId = new Lazy<int>(() =>
				{
					var remoteEndPoint = (IPEndPoint)args.ProxyClient.TcpClient.Client.RemoteEndPoint;

					//If client is localhost get the process id
					if (NetworkHelper.IsLocalIpAddress(remoteEndPoint.Address))
					{
						return NetworkHelper.GetProcessIdFromPort(remoteEndPoint.Port, endpoint.IpV6Enabled);
					}

					//can't access process Id of remote request from remote machine
					return -1;
				});

				try
				{
					// Parse the first line of the HTTP request
					var httpRequestHead = HttpRequestHeadParser.Parse(httpCmd);

					//Read the request headers in to unique and non-unique header collections
					string line;

					while (!string.IsNullOrEmpty(line = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken)))
					{
						var header = line.Split(ProxyConstants.ColonSplit, 2);

						var newHeader = new HttpHeader(header[0], header[1]);

						List<HttpHeader> existingNonUniqueHeaders;
						HttpHeader exisitingHeader;

						// If header exist in non-unique header collection add it there
						if (request.NonUniqueHeaders.TryGetValue(newHeader.Name, out existingNonUniqueHeaders)
							&& existingNonUniqueHeaders != null)
						{
							existingNonUniqueHeaders.Add(newHeader);
						}
						// If header is alread in unique header collection then move both to non-unique collection
						else if (request.Headers.TryGetValue(newHeader.Name, out exisitingHeader)
								 && exisitingHeader != null)
						{
							var nonUniqueHeaders = new List<HttpHeader> { exisitingHeader, newHeader };

							request.NonUniqueHeaders.Add(newHeader.Name, nonUniqueHeaders);
							request.Headers.Remove(newHeader.Name);
						}
						else
						{
							request.Headers.Add(newHeader.Name, newHeader);
						}
					}

					var httpRemoteUri = new Uri(httpsHostName == null ? httpRequestHead.Url
						: string.Concat("https://", request.Host ?? httpsHostName, httpRequestHead.Url));

					request.TargetUri = httpRemoteUri;

					request.Method = httpRequestHead.Method.Trim().ToUpper();
					request.HttpVersion = httpRequestHead.Version;
					args.ProxyClient.ClientStream = clientStream;
					args.ProxyClient.ClientStreamReader = clientStreamReader;
					args.ProxyClient.ClientStreamWriter = clientStreamWriter;

					if (httpsHostName == null && (await CheckAuthorization(clientStreamWriter, request.Headers.Values) == false))
					{

						Dispose(clientStream, clientStreamReader, clientStreamWriter, args);
						break;
					}

					PrepareRequestHeaders(request.Headers, args.WebSession);
					request.Host = request.TargetUri.Authority;

					//If user requested interception do it
					if (BeforeRequest != null)
					{
						var invocationList = BeforeRequest.GetInvocationList();
						var handlerTasks = new Task[invocationList.Length];

						for (var i = 0; i < invocationList.Length; i++)
						{
							handlerTasks[i] = ((Func<object, SessionEventArgs, CancellationToken, Task>)invocationList[i])(null, args, cancellationToken);
						}

						await Task.WhenAny(
							Task.WhenAll(handlerTasks),
							Task.Delay(TaskTimeout, cancellationToken: cancellationToken));
					}

					//if upgrading to websocket then relay the requet without reading the contents
					if (request.UpgradeToWebSocket)
					{
						await TcpHelper.SendRaw(BufferSize, ConnectionTimeOutSeconds, httpRemoteUri,
												httpCmd, httpRequestHead.Version, request.Headers, args.IsHttps,
												SupportedSslProtocols, ValidateServerCertificate,
												SelectClientCertificate,
												clientStream, _tcpConnectionFactory, cancellationToken: cancellationToken);

						Dispose(clientStream, clientStreamReader, clientStreamWriter, args);
						break;
					}

					// Construct the web request that we are going to issue on behalf of the client.
					await HandleHttpSessionRequestInternal(null, args, customUpStreamHttpProxy, customUpStreamHttpsProxy, false, cancellationToken: cancellationToken).ConfigureAwait(false);

					request.CancelRequest = cancellationToken.IsCancellationRequested;

					if (request.CancelRequest)
					{
						break;
					}

					//if connection is closing exit
					if (args.WebSession.Response.KeepAlive == false)
					{
						break;
					}

					// read the next request
					httpCmd = await clientStreamReader.ReadLineAsync(cancellationToken: cancellationToken);

				}
				catch (Exception e)
				{
					ExceptionFunc(new ProxyHttpException("Error occured whilst handling session request", e, args));
					Dispose(clientStream, clientStreamReader, clientStreamWriter, args);
					break;
				}
			}

			connection?.Dispose();
		}

		/// <summary>
		/// Write successfull CONNECT response to client
		/// </summary>
		/// <param name="clientStreamWriter"></param>
		/// <param name="httpVersion"></param>
		/// <returns></returns>
		private async Task WriteConnectResponse(StreamWriter clientStreamWriter, Version httpVersion)
		{
			// Write HTTP response
			await clientStreamWriter.WriteLineAsync($"HTTP/{httpVersion.Major}.{httpVersion.Minor} 200 Connection established");
			
			// Write timestamp header
			await clientStreamWriter.WriteLineAsync($"Timestamp: {DateTime.UtcNow}");
			
			await clientStreamWriter.WriteLineAsync();
			await clientStreamWriter.FlushAsync();
		}

		/// <summary>
		/// prepare the request headers so that we can avoid encodings not parsable by this proxy
		/// </summary>
		/// <param name="requestHeaders"></param>
		/// <param name="webRequest"></param>
		private void PrepareRequestHeaders(Dictionary<string, HttpHeader> requestHeaders, HttpWebClient webRequest)
		{
			foreach (var headerItem in requestHeaders)
			{
				var header = headerItem.Value;

				if (header.Name.Equals("accept-encoding", StringComparison.InvariantCultureIgnoreCase))
				{
					header.Value = "gzip,deflate,zlib";
				}
			}

			FixProxyHeaders(requestHeaders);
			webRequest.Request.Headers = requestHeaders;
		}

		/// <summary>
		/// This is called when the request is PUT/POST to read the body
		/// </summary>
		/// <param name="args">The <see cref="SessionEventArgs"/> instance containing the event data.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private async Task SendClientRequestBody(SessionEventArgs args, CancellationToken cancellationToken = default(CancellationToken))
		{
			// End the operation
			var postStream = args.WebSession.ServerConnection.Stream;

			//send the request body bytes to server
			if (args.WebSession.Request.ContentLength > 0)
			{
				await args.ProxyClient.ClientStreamReader.CopyBytesToStream(BufferSize, postStream, args.WebSession.Request.ContentLength, cancellationToken: cancellationToken);

			}
			//Need to revist, find any potential bugs
			//send the request body bytes to server in chunks
			else if (args.WebSession.Request.IsChunked)
			{
				await args.ProxyClient.ClientStreamReader.CopyBytesToStreamChunked(BufferSize, postStream, cancellationToken: cancellationToken);
			}
		}
	}
}