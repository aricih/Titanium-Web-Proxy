using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Authentication;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Network
{
	internal class TcpClientFactory
	{
		/// <summary>
		/// Creates the HTTP client.
		/// </summary>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="connectionTimeOutSeconds">The connection time out seconds.</param>
		/// <param name="requestUri">The request URI.</param>
		/// <param name="requestHeaders">The request headers.</param>
		/// <param name="httpVersion">The HTTP version.</param>
		/// <param name="supportedSslProtocols">The supported SSL protocols.</param>
		/// <param name="remoteCertificateValidationCallback">The remote certificate validation callback.</param>
		/// <param name="localCertificateSelectionCallback">The local certificate selection callback.</param>
		/// <param name="externalHttpProxy">The external HTTP proxy.</param>
		/// <param name="clientStream">The client stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>TcpClientWrapper instance.</returns>
		internal Task<TcpClientWrapper> CreateHttpClient(int bufferSize, int connectionTimeOutSeconds,
			Uri requestUri,
			IDictionary<string, HttpHeader> requestHeaders,
			Version httpVersion, SslProtocols supportedSslProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback,
			LocalCertificateSelectionCallback localCertificateSelectionCallback,
			ExternalProxy externalHttpProxy,
			Stream clientStream,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			var isProxified = externalHttpProxy != null && externalHttpProxy.HostName != requestUri.Host;

			var result = new TcpClientWrapper
			{
				Client = isProxified
					? new TcpClient(externalHttpProxy.HostName, externalHttpProxy.Port)
					: new TcpClient(requestUri.Host, requestUri.Port)
			};

			if (AuthorizationHeaderCache.HasHost(requestUri.Host))
			{
				AuthenticationClient.PreAuthenticate(requestUri, requestHeaders);
				result.IsPreAuthenticateUsed = true;
			}

			return Task.FromResult(result);
		}

		/// <summary>
		/// Creates the HTTPS client.
		/// </summary>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="connectionTimeOutSeconds">The connection time out seconds.</param>
		/// <param name="requestUri">The request URI.</param>
		/// <param name="requestHeaders">The request headers.</param>
		/// <param name="httpVersion">The HTTP version.</param>
		/// <param name="supportedSslProtocols">The supported SSL protocols.</param>
		/// <param name="remoteCertificateValidationCallback">The remote certificate validation callback.</param>
		/// <param name="localCertificateSelectionCallback">The local certificate selection callback.</param>
		/// <param name="externalHttpsProxy">The external HTTPS proxy.</param>
		/// <param name="clientStream">The client stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Task&lt;TcpClientWrapper&gt;.</returns>
		internal async Task<TcpClientWrapper> CreateHttpsClient(int bufferSize, int connectionTimeOutSeconds,
			Uri requestUri, IDictionary<string, HttpHeader> requestHeaders,
			Version httpVersion, SslProtocols supportedSslProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback,
			LocalCertificateSelectionCallback localCertificateSelectionCallback,
			ExternalProxy externalHttpsProxy,
			Stream clientStream,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			var isProxified = externalHttpsProxy != null && externalHttpsProxy.HostName != requestUri.Host;

			var result = new TcpClientWrapper
			{
				Client = isProxified
					? new TcpClient(externalHttpsProxy.HostName, externalHttpsProxy.Port)
					: new TcpClient(requestUri.Host, requestUri.Port)
			};

			if (AuthorizationHeaderCache.HasHost(requestUri.Host))
			{
				AuthenticationClient.PreAuthenticate(requestUri, requestHeaders);
				result.IsPreAuthenticateUsed = true;
			}

			if (isProxified)
			{
				using (var writer = new StreamWriter(result.Stream, Encoding.ASCII, bufferSize, true))
				{
					await writer.WriteLineAsync($"CONNECT {requestUri.Host}:{requestUri.Port} HTTP/{httpVersion}");
					await writer.WriteLineAsync($"Host: {requestUri.Host}:{requestUri.Port}");
					await writer.WriteLineAsync("Connection: Keep-Alive");
					await writer.WriteLineAsync("Proxy-Connection: Keep-Alive");

					HttpHeaderCollection authorizationHeaderCollection;

					if (AuthorizationHeaderCache.TryGetProxyAuthorizationHeaders(requestUri.Host, out authorizationHeaderCollection) &&
						authorizationHeaderCollection != null)
					{
						foreach (var authorizationHeader in authorizationHeaderCollection.Values)
						{
							await writer.WriteLineAsync($"{authorizationHeader.Name}:{authorizationHeader.Value}");
						}
					}

					await writer.WriteLineAsync();
					await writer.FlushAsync();
					writer.Close();
				}
			}

			if (cancellationToken.IsCancellationRequested)
			{
				return result;
			}

			SslStream sslStream = null;

			try
			{
				sslStream = new SslStream(result.Stream, true, remoteCertificateValidationCallback,
					localCertificateSelectionCallback);

				await sslStream.AuthenticateAsClientAsync(requestUri.Host, null, supportedSslProtocols, false);

				result.Stream = sslStream;
			}
			catch
			{
				sslStream?.Dispose();

				if (isProxified)
				{
					result = new TcpClientWrapper
					{
						Client = new TcpClient(externalHttpsProxy.HostName, externalHttpsProxy.Port)
					};
				}
				else
				{
					throw;
				}
			}

			return result;
		}
	}
}