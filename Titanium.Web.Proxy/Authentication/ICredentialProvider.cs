using System.Net;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Authentication
{
	public interface ICredentialProvider
	{
		Task<NetworkCredential> GetCredentials();
	}
}