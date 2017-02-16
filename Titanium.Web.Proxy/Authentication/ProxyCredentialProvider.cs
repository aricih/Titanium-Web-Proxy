using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Authentication
{
	/// <summary>
	/// Implements credential provider for Proxy Authentication Required (HTTP/407).
	/// </summary>
	/// <seealso cref="Titanium.Web.Proxy.Authentication.ICredentialProvider" />
	public class ProxyCredentialProvider : ICredentialProvider
	{
		private readonly ExternalProxy _proxy;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProxyCredentialProvider"/> class.
		/// </summary>
		/// <param name="externalProxy">The external proxy.</param>
		public ProxyCredentialProvider(ExternalProxy externalProxy)
		{
			_proxy = externalProxy;
		}

		/// <summary>
		/// Gets the credentials.
		/// </summary>
		/// <returns>NetworkCredential instance.</returns>
		public Task<NetworkCredential> GetCredentials()
		{
			return Task.FromResult(_proxy.UseDefaultCredentials ? CredentialCache.DefaultNetworkCredentials : _proxy.Credentials);
		}
	}
}