using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;

namespace Titanium.Web.Proxy
{
	public partial class ProxyServer
	{
		/// <summary>
		/// Call back to override server certificate validation
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		internal bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{

			//By default do not allow this client to communicate with unauthenticated servers.
			if (ServerCertificateValidationCallback == null)
			{
				return sslPolicyErrors == SslPolicyErrors.None;
			}

			var args = new CertificateValidationEventArgs
			{
				Certificate = certificate,
				Chain = chain,
				SslPolicyErrors = sslPolicyErrors
			};

			var invocationList = ServerCertificateValidationCallback.GetInvocationList();
			var handlerTasks = new Task[invocationList.Length];

			for (var i = 0; i < invocationList.Length; i++)
			{
				handlerTasks[i] = ((Func<object, CertificateValidationEventArgs, Task>)invocationList[i])(null, args);
			}

			Task.WhenAll(handlerTasks).Wait();

			return args.IsValid;
		}

		/// <summary>
		/// Call back to select client certificate used for mutual authentication
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="targetHost">The target host.</param>
		/// <param name="localCertificates">The local certificates.</param>
		/// <param name="remoteCertificate">The remote certificate.</param>
		/// <param name="acceptableIssuers">The acceptable issuers.</param>
		/// <returns>X509Certificate.</returns>
		internal X509Certificate SelectClientCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			X509Certificate clientCertificate = null;

			if (acceptableIssuers != null &&
				acceptableIssuers.Length > 0 &&
				localCertificates != null &&
				localCertificates.Count > 0)
			{
				// Use the first certificate that is from an acceptable issuer.
				foreach (var certificate in localCertificates)
				{
					var issuer = certificate.Issuer;
					if (Array.IndexOf(acceptableIssuers, issuer) != -1)
					{
						clientCertificate = certificate;
					}
				}
			}

			if (localCertificates != null &&
				localCertificates.Count > 0)
			{
				clientCertificate = localCertificates[0];
			}

			//If user call back isn't registered
			if (ClientCertificateSelectionCallback == null)
			{
				return clientCertificate;
			}

			var args = new CertificateSelectionEventArgs
			{
				TargetHost = targetHost,
				LocalCertificates = localCertificates,
				RemoteCertificate = remoteCertificate,
				AcceptableIssuers = acceptableIssuers,
				ClientCertificate = clientCertificate
			};

			var invocationList = ClientCertificateSelectionCallback.GetInvocationList();
			var handlerTasks = new Task[invocationList.Length];

			for (var i = 0; i < invocationList.Length; i++)
			{
				handlerTasks[i] = ((Func<object, CertificateSelectionEventArgs, Task>)invocationList[i])(null, args);
			}

			Task.WhenAll(handlerTasks).Wait();

			return args.ClientCertificate;
		}
	}
}
