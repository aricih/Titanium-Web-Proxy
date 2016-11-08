using System.Net;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Authentication
{
	public class HttpCredentialProvider : ICredentialProvider
	{
		private readonly string hostName;

		public HttpCredentialProvider(string requestHostName)
		{
			hostName = requestHostName;
		}

		private bool IsCredentialHandlingEnabled => ProxyServer.Instance.GetCustomHttpCredentialsFunc != null;

		public async Task<NetworkCredential> GetCredentials()
		{
			if (!IsCredentialHandlingEnabled)
			{
				return null;
			}

			return await ProxyServer.Instance.GetCustomHttpCredentialsFunc(hostName);
		}
	}
}