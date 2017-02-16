using System.Collections.Generic;
using System.Net;

namespace Titanium.Web.Proxy.Models
{
	/// <summary>
	/// A proxy endpoint that the client is aware of 
	/// So client application know that it is communicating with a proxy server
	/// </summary>
	public class ExplicitProxyEndPoint : ProxyEndPoint
	{
		internal bool IsSystemHttpProxy { get; set; }

		internal bool IsSystemHttpsProxy { get; set; }

		/// <summary>
		/// Gets or sets the excluded HTTPS host name regex.
		/// </summary>
		public List<string> ExcludedHttpsHostNameRegex { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ExplicitProxyEndPoint"/> class.
		/// </summary>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="port">The port.</param>
		/// <param name="enableSsl">if set to <c>true</c> [enable SSL].</param>
		public ExplicitProxyEndPoint(IPAddress ipAddress, int port, bool enableSsl)
			: base(ipAddress, port, enableSsl)
		{

		}
	}
}