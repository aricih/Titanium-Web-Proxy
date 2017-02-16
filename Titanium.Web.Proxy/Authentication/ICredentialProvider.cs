using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Authentication
{
	/// <summary>
	/// Credential provider interface
	/// </summary>
	public interface ICredentialProvider
	{
		/// <summary>
		/// Gets the credentials.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>NetworkCredential instance.</returns>
		Task<NetworkCredential> GetCredentials();
	}
}