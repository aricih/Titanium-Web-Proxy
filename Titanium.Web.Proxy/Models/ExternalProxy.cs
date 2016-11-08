using System.Net;

namespace Titanium.Web.Proxy.Models
{
	/// <summary>
	/// An upstream proxy this proxy uses if any
	/// </summary>
	public class ExternalProxy
	{
		public NetworkCredential Credentials { get; set; }

		public bool UseDefaultCredentials { get; set; }

		public string HostName { get; set; }

		public int Port { get; set; }
	}
}
