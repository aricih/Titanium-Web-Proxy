using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Net.Security;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Models;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// A class that manages Tcp Connection to server used by this proxy server
	/// </summary>
	internal class TcpConnectionFactory
	{
		private readonly TcpClientFactory _tcpClientFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpConnectionFactory"/> class.
		/// </summary>
		/// <param name="clientFactory">The client factory.</param>
		internal TcpConnectionFactory(TcpClientFactory clientFactory)
		{
			_tcpClientFactory = clientFactory;
		}

		/// <summary>
		/// Creates a TCP connection to server
		/// </summary>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="connectionTimeOutSeconds">The connection time out seconds.</param>
		/// <param name="requestUri">The request URI.</param>
		/// <param name="requestHeaders">The request headers.</param>
		/// <param name="httpVersion">The HTTP version.</param>
		/// <param name="isHttps">if set to <c>true</c> [is HTTPS].</param>
		/// <param name="supportedSslProtocols">The supported SSL protocols.</param>
		/// <param name="remoteCertificateValidationCallback">The remote certificate validation callback.</param>
		/// <param name="localCertificateSelectionCallback">The local certificate selection callback.</param>
		/// <param name="externalHttpProxy">The external HTTP proxy.</param>
		/// <param name="externalHttpsProxy">The external HTTPS proxy.</param>
		/// <param name="clientStream">The client stream.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>TcpConnection instance.</returns>
		/// <exception cref="Exception">Upstream proxy failed to create a secure tunnel</exception>
		internal async Task<TcpConnection> CreateClient(int bufferSize, int connectionTimeOutSeconds,
			Uri requestUri,
			IDictionary<string, HttpHeader> requestHeaders,
			Version httpVersion,
			bool isHttps, SslProtocols supportedSslProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback, LocalCertificateSelectionCallback localCertificateSelectionCallback,
			ExternalProxy externalHttpProxy, ExternalProxy externalHttpsProxy,
			Stream clientStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			var remoteHostName = requestUri.Host;
			var remotePort = requestUri.Port;

			var clientWrapper = isHttps
				? await _tcpClientFactory.CreateHttpsClient(bufferSize,
					connectionTimeOutSeconds,
					requestUri,
					requestHeaders,
					httpVersion,
					supportedSslProtocols,
					remoteCertificateValidationCallback,
					localCertificateSelectionCallback,
					externalHttpsProxy,
					clientStream,
					cancellationToken: cancellationToken)
				: await _tcpClientFactory.CreateHttpClient(bufferSize,
					connectionTimeOutSeconds,
					requestUri,
					requestHeaders,
					httpVersion,
					supportedSslProtocols,
					remoteCertificateValidationCallback,
					localCertificateSelectionCallback,
					externalHttpProxy,
					clientStream,
					cancellationToken: cancellationToken).ConfigureAwait(false);

			clientWrapper.Client.ReceiveTimeout = connectionTimeOutSeconds * 1000;
			clientWrapper.Client.SendTimeout = connectionTimeOutSeconds * 1000;

			clientWrapper.Stream.ReadTimeout = connectionTimeOutSeconds * 1000;
			clientWrapper.Stream.WriteTimeout = connectionTimeOutSeconds * 1000;

			if (ProxyServer.Instance.ForceSimpleAuthentication && ProxyServer.Instance.GetCustomHttpCredentialsFunc != null)
			{
				var credentials = await ProxyServer.Instance.GetCustomHttpCredentialsFunc(requestUri.Host);

				var authorizationHeader = new HttpHeader("Authorization", $"Simple {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.UserName}:{credentials.Password}"))}");

				requestHeaders[authorizationHeader.Name] = authorizationHeader;
			}

			return new TcpConnection
			{
				UpstreamHttpProxy = externalHttpProxy,
				UpstreamHttpsProxy = externalHttpsProxy,
				HostName = remoteHostName,
				Port = remotePort,
				IsHttps = isHttps,
				TcpClient = clientWrapper.Client,
				StreamReader = new CustomBinaryReader(clientWrapper.Stream),
				Stream = clientWrapper.Stream,
				Version = httpVersion,
				PreAuthenticateUsed = clientWrapper.IsPreAuthenticateUsed
			};
		}
	}
}