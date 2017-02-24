using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using System.Linq;
using System.Security.Authentication;
using System.Threading;

namespace Titanium.Web.Proxy
{
	/// <summary>
	///     Proxy Server Main class
	/// </summary>
	public partial class ProxyServer : IDisposable
	{

		/// <summary>
		/// Is the root certificate used by this proxy is valid?
		/// </summary>
		private bool _certValidated;

		/// <summary>
		/// Is the proxy currently running
		/// </summary>
		private bool _proxyRunning;

		/// <summary>
		/// Manages certificates used by this proxy
		/// </summary>
		private CertificateManager _certificateCacheManager;

		/// <summary>
		/// An default exception log func
		/// </summary>
		private readonly Lazy<Action<Exception>> _defaultExceptionFunc = new Lazy<Action<Exception>>(() => (e => { }));

		/// <summary>
		/// backing exception func for exposed public property
		/// </summary>
		private Action<Exception> _exceptionFunc;

		/// <summary>
		/// A object that creates tcp connection to server
		/// </summary>
		private readonly TcpConnectionFactory _tcpConnectionFactory;

		/// <summary>
		/// Manage system proxy settings
		/// </summary>
		private readonly SystemProxyManager _systemProxySettingsManager;

		/// <summary>
		/// Gets or sets the firefox proxy settings manager.
		/// </summary>
		private readonly FireFoxProxySettingsManager _firefoxProxySettingsManager;

		/// <summary>
		/// Buffer size used throughout this proxy
		/// </summary>
		public int BufferSize { get; set; } = 8192;

		/// <summary>
		/// Name of the root certificate issuer
		/// </summary>
		public string RootCertificateIssuerName { get; set; }

		/// <summary>
		/// Name of the root certificate
		/// If no certificate is provided then a default Root Certificate will be created and used
		/// The provided root certificate has to be in the proxy exe directory with the private key 
		/// The root certificate file should be named as  "rootCert.pfx"
		/// </summary>
		public string RootCertificateName { get; set; }

		/// <summary>
		/// Gets or sets the name of the root certificate friendly.
		/// </summary>
		public string RootCertificateFriendlyName { get; set; }

		/// <summary>
		/// Trust the RootCertificate used by this proxy server
		/// Note that this do not make the client trust the certificate!
		/// This would import the root certificate to the certificate store of machine that runs this proxy server
		/// </summary>
		public bool TrustRootCertificate { get; set; }

		/// <summary>
		/// Does this proxy uses the HTTP protocol 100 continue behaviour strictly?
		/// Broken 100 contunue implementations on server/client may cause problems if enabled
		/// </summary>
		public bool Enable100ContinueBehaviour { get; set; }

		/// <summary>
		/// Minutes certificates should be kept in cache when not used
		/// </summary>
		public static int CertificateCacheTimeOutMinutes { get; set; } = 60;

		/// <summary>
		/// Seconds client/server connection are to be kept alive when waiting for read/write to complete
		/// </summary>
		public static int ConnectionTimeOutSeconds { get; set; } = 120;

		/// <summary>
		/// Intercept request to server
		/// </summary>
		public event Func<object, SessionEventArgs, CancellationToken, Task> BeforeRequest;

		/// <summary>
		/// Intercept response from server
		/// </summary>
		public event Func<object, SessionEventArgs, CancellationToken, Task> BeforeResponse;

		/// <summary>
		/// External proxy for Http
		/// </summary>
		public ExternalProxy UpStreamHttpProxy { get; set; }

		/// <summary>
		/// External proxy for Http
		/// </summary>
		public ExternalProxy UpStreamHttpsProxy { get; set; }

		/// <summary>
		/// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication
		/// </summary>
		public event Func<object, CertificateValidationEventArgs, Task> ServerCertificateValidationCallback;

		/// <summary>
		/// Callback tooverride client certificate during SSL mutual authentication
		/// </summary>
		public event Func<object, CertificateSelectionEventArgs, Task> ClientCertificateSelectionCallback;

		/// <summary>
		/// The task timeout
		/// </summary>
		internal static readonly TimeSpan TaskTimeout = TimeSpan.FromSeconds(ConnectionTimeOutSeconds);

		/// <summary>
		/// Callback for error events in proxy
		/// </summary>
		public Action<Exception> ExceptionFunc
		{
			get
			{
				return _exceptionFunc ?? _defaultExceptionFunc.Value;
			}
			set
			{
				_exceptionFunc = value;
			}
		}

		/// <summary>
		/// A callback to authenticate clients 
		/// Parameters are username, password provided by client
		/// return true for successful authentication
		/// </summary>
		public Func<string, string, Task<bool>> AuthenticateUserFunc
		{
			get;
			set;
		}

		/// <summary>
		/// A callback to provide authentication credentials for up stream proxy this proxy is using for HTTP requests
		/// return the ExternalProxy object with valid credentials
		/// </summary>
		public Func<SessionEventArgs, Task<ExternalProxy>> GetCustomUpStreamHttpProxyFunc
		{
			get;
			set;
		}

		/// <summary>
		/// A callback to provide authentication credentials for up stream proxy this proxy is using for HTTPS requests
		/// return the ExternalProxy object with valid credentials
		/// </summary>
		public Func<SessionEventArgs, Task<ExternalProxy>> GetCustomUpStreamHttpsProxyFunc
		{
			get;
			set;
		}

		/// <summary>
		/// A callback to provide authentication credentials for HTTP/401 Unauthorized responses
		/// </summary>
		public Func<string, Task<NetworkCredential>> GetCustomHttpCredentialsFunc { get; set; }

		/// <summary>
		/// A list of IpAddress and port this proxy is listening to
		/// </summary>
		public List<ProxyEndpoint> ProxyEndPoints { get; set; }

		/// <summary>
		/// List of supported Ssl versions
		/// </summary>
		public SslProtocols SupportedSslProtocols { get; set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Ssl3;

		/// <summary>
		/// Is the proxy currently running
		/// </summary>
		public bool ProxyRunning => _proxyRunning;

		/// <summary>
		/// Gets or sets a value indicating whether requests will be chained to upstream gateway.
		/// </summary>
		public bool ForwardToUpstreamGateway { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether simple authentication will be forced on HTTP 401.
		/// </summary>
		public bool ForceSimpleAuthentication { get; set; }

		/// <summary>
		/// Gets or sets the default name of the root certificate.
		/// </summary>
		public static string DefaultRootCertificateName { get; set; } = "Titanium Root Certificate Authority";

		/// <summary>
		/// Gets or sets the default name of the root certificate issuer.
		/// </summary>
		public static string DefaultRootCertificateIssuerName { get; set; } = "Titanium";

		/// <summary>
		/// Gets or sets the default name of the root certificate friendly.
		/// </summary>
		public static string DefaultRootCertificateFriendlyName { get; set; } = "DO_NOT_TRUST_TitaniumProxy-CE";

		/// <summary>
		/// Prevents a default instance of the <see cref="ProxyServer"/> class from being created.
		/// </summary>
		private ProxyServer() : this(null, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProxyServer"/> class.
		/// </summary>
		/// <param name="rootCertificateName">Name of the root certificate.</param>
		/// <param name="rootCertificateIssuerName">Name of the root certificate issuer.</param>
		private ProxyServer(string rootCertificateName, string rootCertificateIssuerName)
		{
			RootCertificateName = rootCertificateName;
			RootCertificateIssuerName = rootCertificateIssuerName;

			ProxyEndPoints = new List<ProxyEndpoint>();

			var tcpClientFactory = new TcpClientFactory();
			_tcpConnectionFactory = new TcpConnectionFactory(tcpClientFactory);
			_systemProxySettingsManager = new SystemProxyManager();
			_firefoxProxySettingsManager = new FireFoxProxySettingsManager();

			RootCertificateName = RootCertificateName ?? DefaultRootCertificateName;
			RootCertificateIssuerName = RootCertificateIssuerName ?? DefaultRootCertificateIssuerName;
		}

		/// <summary>
		/// The singleton instance
		/// </summary>
		private static readonly Lazy<ProxyServer> Singleton = new Lazy<ProxyServer>(() => new ProxyServer(DefaultRootCertificateName, DefaultRootCertificateIssuerName));

		/// <summary>
		/// Gets the singleton instance.
		/// </summary>
		public static ProxyServer Instance => Singleton.Value;

		/// <summary>
		/// Add a proxy end point
		/// </summary>
		/// <param name="endpoint">The end point.</param>
		/// <param name="cancellationToken">The cancellation token. 
		/// This token is effective if only the endpoint is added after the proxy server is started.
		/// Otherwise endpoints will be using the token which is passed on Start call.</param>
		/// <exception cref="Exception">Cannot add another endpoint to same port and ip address</exception>
		public void AddEndPoint(ProxyEndpoint endpoint, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (ProxyEndPoints.Any(x => x.IpAddress.Equals(endpoint.IpAddress) && endpoint.Port != 0 && x.Port == endpoint.Port))
			{
				throw new Exception("Cannot add another endpoint to same port & ip address");
			}

			ProxyEndPoints.Add(endpoint);

			if (_proxyRunning)
			{
				Listen(endpoint, cancellationToken: cancellationToken);
			}
		}

		/// <summary>
		/// Remove a proxy end point
		/// Will throw error if the end point does'nt exist 
		/// </summary>
		/// <param name="endpoint"></param>
		public void RemoveEndPoint(ProxyEndpoint endpoint)
		{
			if (ProxyEndPoints.Contains(endpoint) == false)
			{
				throw new Exception("Cannot remove endPoints not added to proxy");
			}

			ProxyEndPoints.Remove(endpoint);

			if (_proxyRunning)
			{
				QuitListen(endpoint);
			}
		}

		/// <summary>
		/// Set the given explicit end point as the default proxy server for current machine
		/// </summary>
		/// <param name="endpoint"></param>
		public void SetAsSystemHttpProxy(ExplicitProxyEndpoint endpoint)
		{
			ValidateEndPointAsSystemProxy(endpoint);

			//clear any settings previously added
			ProxyEndPoints.OfType<ExplicitProxyEndpoint>().ToList().ForEach(x => x.IsSystemHttpProxy = false);

			_systemProxySettingsManager.SetHttpProxy(
				Equals(endpoint.IpAddress, IPAddress.Any) | Equals(endpoint.IpAddress, IPAddress.Loopback) ? "127.0.0.1" : endpoint.IpAddress.ToString(), endpoint.Port);

			endpoint.IsSystemHttpProxy = true;
#if !DEBUG
			_firefoxProxySettingsManager.AddProxyToFirefoxConfiguration();
#endif
		}

		/// <summary>
		/// Set the given explicit end point as the default proxy server for current machine
		/// </summary>
		/// <param name="endpoint"></param>
		public void SetAsSystemHttpsProxy(ExplicitProxyEndpoint endpoint)
		{
			ValidateEndPointAsSystemProxy(endpoint);

			if (!endpoint.EnableSsl)
			{
				throw new Exception("Endpoint do not support Https connections");
			}

			//clear any settings previously added
			ProxyEndPoints.OfType<ExplicitProxyEndpoint>().ToList().ForEach(x => x.IsSystemHttpsProxy = false);


			//If certificate was trusted by the machine
			if (_certValidated)
			{
				_systemProxySettingsManager.SetHttpsProxy(
				   Equals(endpoint.IpAddress, IPAddress.Any) | Equals(endpoint.IpAddress, IPAddress.Loopback) ? "127.0.0.1" : endpoint.IpAddress.ToString(),
					endpoint.Port);
			}


			endpoint.IsSystemHttpsProxy = true;

#if !DEBUG
			_firefoxProxySettingsManager.AddProxyToFirefoxConfiguration();
#endif
		}

		/// <summary>
		/// Remove any HTTP proxy setting of current machien
		/// </summary>
		public void DisableSystemHttpProxy()
		{
			_systemProxySettingsManager.RemoveHttpProxy();
		}

		/// <summary>
		/// Remove any HTTPS proxy setting for current machine
		/// </summary>
		public void DisableSystemHttpsProxy()
		{
			_systemProxySettingsManager.RemoveHttpsProxy();
		}

		/// <summary>
		/// Clear all proxy settings for current machine
		/// </summary>
		public void DisableAllSystemProxies()
		{
			_systemProxySettingsManager.DisableAllProxy();
		}

		/// <summary>
		/// Start this proxy server
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="Exception">Proxy is already running.</exception>
		public void Start(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (_proxyRunning)
			{
				throw new Exception("Proxy is already running.");
			}

			RequestBodyCache.Clear();

			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			_certificateCacheManager = new CertificateManager(RootCertificateIssuerName,
				RootCertificateName, ExceptionFunc);

			_certValidated = _certificateCacheManager.CreateTrustedRootCertificate();

			if (TrustRootCertificate)
			{
				_certificateCacheManager.TrustRootCertificate();
			}

			if (ForwardToUpstreamGateway && GetCustomUpStreamHttpProxyFunc == null && GetCustomUpStreamHttpsProxyFunc == null)
			{
				GetCustomUpStreamHttpProxyFunc = GetSystemUpStreamProxy;
				GetCustomUpStreamHttpsProxyFunc = GetSystemUpStreamProxy;
			}

			foreach (var endPoint in ProxyEndPoints)
			{
				Listen(endPoint, cancellationToken: cancellationToken);
			}

			_certificateCacheManager.ClearIdleCertificates(CertificateCacheTimeOutMinutes);

			_proxyRunning = true;
		}

		/// <summary>
		/// Gets the system up stream proxy.
		/// </summary>
		/// <param name="sessionEventArgs">The <see cref="SessionEventArgs"/> instance containing the event data.</param>
		/// <returns><see cref="ExternalProxy"/> instance containing valid proxy configuration from PAC/WAPD scripts if any exists.</returns>
		private Task<ExternalProxy> GetSystemUpStreamProxy(SessionEventArgs sessionEventArgs)
		{
			var systemProxyRegistryValue =
				_systemProxySettingsManager.GetSystemProxy(sessionEventArgs.IsHttps
					? ProxyProtocolType.Https
					: ProxyProtocolType.Http);

			ExternalProxy systemProxy;

			if (systemProxyRegistryValue != null)
			{
				systemProxy = HasSocket(systemProxyRegistryValue.HostName, systemProxyRegistryValue.Port)
					? null
					: new ExternalProxy
					{
						HostName = systemProxyRegistryValue.HostName,
						Port = systemProxyRegistryValue.Port,
						UseDefaultCredentials = true
					};
			}
			else
			{
				// Use built-in WebProxy class to handle PAC/WAPD scripts.
				var systemProxyResolver = new WebProxy();

				var systemProxyUri = systemProxyResolver.GetProxy(sessionEventArgs.WebSession.Request.TargetUri);

				systemProxy = systemProxyUri.Host.Equals(sessionEventArgs.WebSession.Request.Host,
					StringComparison.InvariantCultureIgnoreCase)
					? null
					: new ExternalProxy
					{
						HostName = systemProxyUri.Host,
						Port = systemProxyUri.Port,
						UseDefaultCredentials = true
					};
			}

			return Task.FromResult(systemProxy);
		}

		/// <summary>
		/// Stop this proxy server
		/// </summary>
		public void Stop()
		{
			if (!_proxyRunning)
			{
				throw new Exception("Proxy is not running.");
			}

			var setAsSystemProxy = ProxyEndPoints.OfType<ExplicitProxyEndpoint>().Any(x => x.IsSystemHttpProxy || x.IsSystemHttpsProxy);

			if (setAsSystemProxy)
			{
				_systemProxySettingsManager.DisableAllProxy();
#if !DEBUG
				_firefoxProxySettingsManager.RemoveProxyFromFirefoxConfiguration();
#endif
			}

			foreach (var endPoint in ProxyEndPoints)
			{
				QuitListen(endPoint);
			}

			ProxyEndPoints.Clear();

			_certificateCacheManager?.StopClearIdleCertificates();

			RequestBodyCache.Clear();

			_proxyRunning = false;
		}

		/// <summary>
		/// Determines whether the specified socket belongs to the proxy server.
		/// </summary>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="port">The port.</param>
		/// <returns><c>true</c> if the proxy server has has the specified socket; otherwise, <c>false</c>.</returns>
		internal bool HasSocket(IPAddress ipAddress, int port)
		{
			return ProxyEndPoints.Any(endPoint => (endPoint.IpAddress.Equals(IPAddress.Any)
				|| endPoint.IpAddress.Equals(IPAddress.IPv6Any)
				|| endPoint.IpAddress.Equals(ipAddress))
			&& port != 0 && endPoint.Port == port);
		}

		/// <summary>
		/// Determines whether the specified socket belongs to the proxy server.
		/// </summary>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="port">The port.</param>
		/// <returns><c>true</c> if the proxy server has has the specified socket; otherwise, <c>false</c>.</returns>
		internal bool HasSocket(string hostName, int port)
		{
			var ipAddresses = Dns.GetHostAddresses(hostName);

			return ipAddresses.Any(ipAddress => HasSocket(ipAddress, port));
		}

		/// <summary>
		/// Listen on the given end point on local machine
		/// </summary>
		/// <param name="endpoint">The end point.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private void Listen(ProxyEndpoint endpoint, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				endpoint.Listener = new TcpListener(endpoint.IpAddress, endpoint.Port);
				endpoint.Listener.Start();

				endpoint.Port = ((IPEndPoint) endpoint.Listener.LocalEndpoint).Port;
				endpoint.CancellationToken = cancellationToken;

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				// Accept clients asynchronously
				endpoint.Listener.BeginAcceptTcpClient(OnAcceptConnection, endpoint);
			}
			catch (SocketException e)
			{
				ExceptionFunc(e);
				QuitListen(endpoint);
			}
		}

		/// <summary>
		/// Quit listening on the given end point
		/// </summary>
		/// <param name="endpoint"></param>
		private void QuitListen(ProxyEndpoint endpoint)
		{
			endpoint.Listener.Stop();
			endpoint.Listener.Server.Close();
			endpoint.Listener.Server.Dispose();
		}

		/// <summary>
		/// Verifiy if its safe to set this end point as System proxy
		/// </summary>
		/// <param name="endpoint"></param>
		private void ValidateEndPointAsSystemProxy(ExplicitProxyEndpoint endpoint)
		{
			if (ProxyEndPoints.Contains(endpoint) == false)
			{
				throw new Exception("Cannot set endPoints not added to proxy as system proxy");
			}

			if (!_proxyRunning)
			{
				throw new Exception("Cannot set system proxy settings before proxy has been started.");
			}
		}

		/// <summary>
		/// When a connection is received from client act
		/// </summary>
		/// <param name="asyncResult"></param>
		private void OnAcceptConnection(IAsyncResult asyncResult)
		{
			var endPoint = (ProxyEndpoint)asyncResult.AsyncState;

			TcpClient tcpClient = null;

			try
			{
				// Based on end point type call appropriate request handlers
				tcpClient = endPoint.Listener.EndAcceptTcpClient(asyncResult);
			}
			catch (ObjectDisposedException)
			{
				// The listener was Stop()'d, disposing the underlying socket and
				// triggering the completion of the callback. We're already exiting,
				// so just return.
				return;
			}
			catch
			{
				//Other errors are discarded to keep proxy running
			}


			if (tcpClient != null)
			{
				Task.Run(async () =>
				{
					try
					{
						switch (endPoint.EndpointType)
						{
							case EndpointType.Explicit:
								await HandleClient(endPoint as ExplicitProxyEndpoint, tcpClient, cancellationToken: endPoint.CancellationToken);
								break;
							case EndpointType.Transparent:
								await HandleClient(endPoint as TransparentProxyEndpoint, tcpClient, cancellationToken: endPoint.CancellationToken);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(endPoint.EndpointType), "Unknown endpoint type");
						}
					}
					finally
					{
						if (tcpClient != null)
						{
							//This line is important!
							//contributors please don't remove it without discussion
							//It helps to avoid eventual deterioration of performance due to TCP port exhaustion
							//due to default TCP CLOSE_WAIT timeout for 4 minutes
							tcpClient.LingerState = new LingerOption(true, 0);

							tcpClient.Client.Shutdown(SocketShutdown.Both);
							tcpClient.Client.Close();
							tcpClient.Client.Dispose();
							tcpClient.Close();
						}
					}
				});
			}

			// Get the listener that handles the client request.
			endPoint.Listener.BeginAcceptTcpClient(OnAcceptConnection, endPoint);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (_proxyRunning)
			{
				Stop();
			}

			_certificateCacheManager?.Dispose();
		}
	}
}