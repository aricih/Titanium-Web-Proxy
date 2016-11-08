using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Authentication;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Network
{
	internal class TcpClientFactory
	{
		internal async Task<TcpClientWrapper> CreateHttpClient(int bufferSize, int connectionTimeOutSeconds,
			Uri requestUri,
			IDictionary<string, HttpHeader> requestHeaders,
			Version httpVersion, SslProtocols supportedSslProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback,
			LocalCertificateSelectionCallback localCertificateSelectionCallback,
			ExternalProxy externalHttpProxy,
			Stream clientStream)
		{
			var isProxified = (externalHttpProxy != null && externalHttpProxy.HostName != requestUri.Host);

			var result = new TcpClientWrapper
			{
				Client = isProxified
					? new TcpClient(externalHttpProxy.HostName, externalHttpProxy.Port)
					: new TcpClient(requestUri.Host, requestUri.Port)
			};

			if (AuthorizationHeaderCache.HasHost(requestUri.Host))
			{
				await AuthenticationClient.PreAuthenticate(requestUri, requestHeaders);
			}

			return result;
		}

		internal async Task<TcpClientWrapper> CreateHttpsClient(int bufferSize, int connectionTimeOutSeconds,
			Uri requestUri, IDictionary<string, HttpHeader> requestHeaders,
			Version httpVersion, SslProtocols supportedSslProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback,
			LocalCertificateSelectionCallback localCertificateSelectionCallback,
			ExternalProxy externalHttpsProxy,
			Stream clientStream)
		{
			var isProxified = (externalHttpsProxy != null && externalHttpsProxy.HostName != requestUri.Host);

			var result = new TcpClientWrapper
			{
				Client = isProxified
					? new TcpClient(externalHttpsProxy.HostName, externalHttpsProxy.Port)
					: new TcpClient(requestUri.Host, requestUri.Port)
			};

			if (!isProxified)
			{
				return result;
			}

			using (var writer = new StreamWriter(result.Stream, Encoding.ASCII, bufferSize, true))
			{
				await writer.WriteLineAsync($"CONNECT {requestUri.Host}:{requestUri.Port} HTTP/{httpVersion}");
				await writer.WriteLineAsync($"Host: {requestUri.Host}:{requestUri.Port}");
				await writer.WriteLineAsync("Connection: Keep-Alive");
				await writer.WriteLineAsync("Proxy-Connection: Keep-Alive");

				HttpHeaderCollection authorizationHeaderCollection;

				if (AuthorizationHeaderCache.TryGetProxyAuthorizationHeaders(requestUri.Host, out authorizationHeaderCollection) && authorizationHeaderCollection != null)
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

				// Failed to create ssl stream return a new connection for authentication
				result = new TcpClientWrapper
				{
					Client = new TcpClient(externalHttpsProxy.HostName, externalHttpsProxy.Port)
				};
			}

			return result;
		}
	}
}