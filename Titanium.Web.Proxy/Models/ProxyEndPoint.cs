using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Titanium.Web.Proxy.Models
{
	/// <summary>
	/// An abstract endpoint where the proxy listens
	/// </summary>
	public abstract class ProxyEndpoint
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProxyEndpoint"/> class.
		/// </summary>
		/// <param name="ipAddress">The ip address.</param>
		/// <param name="port">The port.</param>
		/// <param name="enableSsl">if set to <c>true</c> [enable SSL].</param>
		protected ProxyEndpoint(IPAddress ipAddress, int port, bool enableSsl)
		{
			IpAddress = ipAddress;
			Port = port;
			EnableSsl = enableSsl;
		}

		/// <summary>
		/// Gets the ip address.
		/// </summary>
		public IPAddress IpAddress { get; internal set; }

		/// <summary>
		/// Gets the port.
		/// </summary>
		public int Port { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether [enable SSL].
		/// </summary>
		public bool EnableSsl { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether [ip v6 enabled].
		/// </summary>
		public bool IpV6Enabled => IpAddress.AddressFamily == AddressFamily.InterNetworkV6;

		/// <summary>
		/// Gets or sets the listener.
		/// </summary>
		internal TcpListener Listener { get; set; }

		/// <summary>
		/// Gets or sets the cancellation token.
		/// </summary>
		internal CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Gets the endpoint type.
		/// </summary>
		internal abstract EndpointType EndpointType { get; }
	}
}