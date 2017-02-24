using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Examples.Basic
{
	public class ProxyTestController
	{
		private readonly ProxyServer _proxyServer;
		private readonly CancellationToken _cancellationToken;

		public ProxyTestController()
		{
			_proxyServer = ProxyServer.Instance;
			_proxyServer.TrustRootCertificate = true;
			_proxyServer.ForwardToUpstreamGateway = true;
			_proxyServer.RootCertificateIssuerName = "PerformanceCert";
			_proxyServer.RootCertificateName = "PerformanceCert";
			_proxyServer.RootCertificateFriendlyName = "PerformanceCert";

			_cancellationToken = new CancellationToken(false);
		}

		public void StartProxy()
		{
			_proxyServer.BeforeRequest += OnRequest;
			_proxyServer.BeforeResponse += OnResponse;
			_proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
			_proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

			//Exclude Https addresses you don't want to proxy
			//Usefull for clients that use certificate pinning
			//for example dropbox.com
			var explicitEndPoint = new ExplicitProxyEndpoint(IPAddress.Any, 8000, true)
			{
				ExcludedHttpsHostNameRegex = new List<string>() { "localhost" }
			};

			//An explicit endpoint is where the client knows about the existance of a proxy
			//So client sends request in a proxy friendly manner
			_proxyServer.AddEndPoint(explicitEndPoint);

			_proxyServer.Start(cancellationToken: _cancellationToken);

			//Only explicit proxies can be set as system proxy!
			_proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
			_proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
		}

		public void Stop()
		{
			_proxyServer.BeforeRequest -= OnRequest;
			_proxyServer.BeforeResponse -= OnResponse;
			_proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
			_proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

			_proxyServer.Stop();
		}

		//intecept & cancel, redirect or update requests
		public async Task OnRequest(object sender, SessionEventArgs e, CancellationToken cancellationToken = default(CancellationToken))
		{
			Console.WriteLine(e.WebSession.Request.Url);
		}

		//Modify response
		public async Task OnResponse(object sender, SessionEventArgs e, CancellationToken cancellationToken = default(CancellationToken))
		{
			// print out process id of current session
			Console.WriteLine($"PID: {e.WebSession.ProcessId.Value}");
			Console.WriteLine($"Elapsed Time: {e.WebSession.Response.ResponseReceived - e.WebSession.Request.RequestBegin}");
		}

		/// <summary>
		/// Allows overriding default certificate validation logic
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
		{
			e.IsValid = true;

			return Task.FromResult(0);
		}

		/// <summary>
		/// Allows overriding default client certificate selection logic during mutual authentication
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
		{
			return Task.FromResult(0);
		}
	}
}