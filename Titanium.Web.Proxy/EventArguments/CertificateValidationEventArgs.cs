using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Titanium.Web.Proxy.EventArguments
{
	/// <summary>
	/// An argument passed on to the user for validating the server certificate during SSL authentication
	/// </summary>
	public class CertificateValidationEventArgs : EventArgs, IDisposable
	{
		/// <summary>
		/// Gets the certificate.
		/// </summary>
		public X509Certificate Certificate { get; internal set; }

		/// <summary>
		/// Gets the chain.
		/// </summary>
		public X509Chain Chain { get; internal set; }

		/// <summary>
		/// Gets the SSL policy errors.
		/// </summary>
		public SslPolicyErrors SslPolicyErrors { get; internal set; }

		/// <summary>
		/// Returns true if certificate is valid.
		/// </summary>
		public bool IsValid { get; set; }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{

		}
	}
}
