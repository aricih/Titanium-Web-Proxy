using System;
using System.Security.Cryptography.X509Certificates;

namespace Titanium.Web.Proxy.EventArguments
{
	/// <summary>
	/// An argument passed on to user for client certificate selection during mutual SSL authentication
	/// </summary>
	public class CertificateSelectionEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the sender.
		/// </summary>
		public object Sender { get; internal set; }

		/// <summary>
		/// Gets the target host.
		/// </summary>
		public string TargetHost { get; internal set; }

		/// <summary>
		/// Gets the local certificates.
		/// </summary>
		public X509CertificateCollection LocalCertificates { get; internal set; }

		/// <summary>
		/// Gets the remote certificate.
		/// </summary>
		public X509Certificate RemoteCertificate { get; internal set; }

		/// <summary>
		/// Gets the acceptable issuers.
		/// </summary>
		public string[] AcceptableIssuers { get; internal set; }

		/// <summary>
		/// Gets or sets the client certificate.
		/// </summary>
		public X509Certificate ClientCertificate { get; set; }

	}
}
