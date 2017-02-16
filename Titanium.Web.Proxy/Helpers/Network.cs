using System.Linq;
using System.Net;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Implements helper methods for network layer operations.
	/// </summary>
	internal class NetworkHelper
	{
		/// <summary>
		/// Finds the process identifier from local port.
		/// </summary>
		/// <param name="port">The port.</param>
		/// <param name="ipVersion">The ip version.</param>
		/// <returns>Process ID.</returns>
		private static int FindProcessIdFromLocalPort(int port, IpVersion ipVersion)
		{
			var tcpRow = TcpHelper.GetExtendedTcpTable(ipVersion).FirstOrDefault(
					row => row.LocalEndPoint.Port == port);

			return tcpRow?.ProcessId ?? 0;
		}

		/// <summary>
		/// Gets the process identifier from port.
		/// </summary>
		/// <param name="port">The port.</param>
		/// <param name="ipV6Enabled">if set to <c>true</c> [ip v6 enabled].</param>
		/// <returns>Process ID.</returns>
		internal static int GetProcessIdFromPort(int port, bool ipV6Enabled)
		{
			var processId = FindProcessIdFromLocalPort(port, IpVersion.Ipv4);

			if (processId > 0 && !ipV6Enabled)
			{
				return processId;
			}

			return FindProcessIdFromLocalPort(port, IpVersion.Ipv6);
		}

		/// <summary>
		/// Determines whether the specified address is local ip address.
		/// </summary>
		/// <param name="address">The address.</param>
		/// <returns><c>true</c> if the specified address is local ip address; otherwise, <c>false</c>.</returns>
		internal static bool IsLocalIpAddress(IPAddress address)
		{
			try
			{
				// get local IP addresses
				var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

				// test if any host IP equals to any local IP or to localhost

				// is localhost
				if (IPAddress.IsLoopback(address))
				{
					return true;
				}

				// is local address
				if (localIPs.Contains(address))
				{
					return true;
				}

			}
			// Ignore any exception and return false
			catch
			{ }

			return false;
		}
	}
}
