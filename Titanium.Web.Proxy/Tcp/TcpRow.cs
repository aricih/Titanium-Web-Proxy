using System.Net;

namespace Titanium.Web.Proxy.Tcp
{
	/// <summary>
	/// Represents a managed interface of IP Helper API TcpRow struct
	/// <see>
	/// <cref>http://msdn2.microsoft.com/en-us/library/aa366913.aspx</cref>
	/// </see>
	/// </summary>
	internal class TcpRow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TcpRow"/> class.
		/// </summary>
		/// <param name="tcpRow">TcpRow struct.</param>
		public TcpRow(Helpers.TcpRow tcpRow)
		{
			ProcessId = tcpRow.owningPid;

			var localPort = (tcpRow.localPort1 << 8) + (tcpRow.localPort2) + (tcpRow.localPort3 << 24) + (tcpRow.localPort4 << 16);
			var localAddress = tcpRow.localAddr;
			LocalEndPoint = new IPEndPoint(localAddress, localPort);

			var remotePort = (tcpRow.remotePort1 << 8) + (tcpRow.remotePort2) + (tcpRow.remotePort3 << 24) + (tcpRow.remotePort4 << 16);
			var remoteAddress = tcpRow.remoteAddr;
			RemoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
		}

		/// <summary>
		/// Gets the local end point.
		/// </summary>
		public IPEndPoint LocalEndPoint { get; private set; }

		/// <summary>
		/// Gets the remote end point.
		/// </summary>
		public IPEndPoint RemoteEndPoint { get; private set; }

		/// <summary>
		/// Gets the process identifier.
		/// </summary>
		public int ProcessId { get; private set; }
	}
}