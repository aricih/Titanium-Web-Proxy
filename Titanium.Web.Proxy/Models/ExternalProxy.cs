using System.Net;

namespace Titanium.Web.Proxy.Models
{
	/// <summary>
	/// An upstream proxy this proxy uses if any
	/// </summary>
	public class ExternalProxy
	{
		/// <summary>
		/// Gets or sets the credentials.
		/// </summary>
		public NetworkCredential Credentials { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [use default credentials].
		/// </summary>
		public bool UseDefaultCredentials { get; set; }

		/// <summary>
		/// Gets or sets the name of the host.
		/// </summary>
		public string HostName { get; set; }

		/// <summary>
		/// Gets or sets the port.
		/// </summary>
		public int Port { get; set; }
	}
}
