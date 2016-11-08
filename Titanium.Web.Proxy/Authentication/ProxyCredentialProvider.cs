using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Authentication
{
	public class ProxyCredentialProvider : ICredentialProvider
	{
		private readonly ExternalProxy proxy;

		public ProxyCredentialProvider(ExternalProxy externalProxy)
		{
			proxy = externalProxy;
		}

		public Task<NetworkCredential> GetCredentials()
		{
			return Task.FromResult(proxy.Credentials ?? CredentialCache.DefaultNetworkCredentials);
		}
	}
}