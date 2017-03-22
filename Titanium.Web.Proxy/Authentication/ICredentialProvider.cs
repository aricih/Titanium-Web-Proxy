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
		/// <returns>NetworkCredential instance.</returns>
		Task<NetworkCredential> GetCredentials();
	}
}