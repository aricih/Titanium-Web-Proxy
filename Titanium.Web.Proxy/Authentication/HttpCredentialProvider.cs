using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Authentication
{
	/// <summary>
	/// Implements credential provider for HTTP/401.
	/// </summary>
	/// <seealso cref="Titanium.Web.Proxy.Authentication.ICredentialProvider" />
	public class HttpCredentialProvider : ICredentialProvider
	{
		private readonly string _hostName;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpCredentialProvider"/> class.
		/// </summary>
		/// <param name="requestHostName">Name of the request host.</param>
		public HttpCredentialProvider(string requestHostName)
		{
			_hostName = requestHostName;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is credential handling enabled.
		/// </summary>
		private static bool IsCredentialHandlingEnabled => ProxyServer.Instance.GetCustomHttpCredentialsFunc != null;

		/// <summary>
		/// Gets the credentials.
		/// </summary>
		/// <returns>NetworkCredential instance.</returns>
		public async Task<NetworkCredential> GetCredentials()
		{
			if (!IsCredentialHandlingEnabled)
			{
				return null;
			}

			return await ProxyServer.Instance.GetCustomHttpCredentialsFunc(_hostName);
		}
	}
}