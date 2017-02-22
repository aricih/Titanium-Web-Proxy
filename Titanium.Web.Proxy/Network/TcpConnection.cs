using System;
using System.IO;
using System.Net.Sockets;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// An object that holds TcpConnection to a particular server and port
	/// </summary>
	public class TcpConnection : IDisposable
	{
		/// <summary>
		/// Gets or sets up stream HTTP proxy.
		/// </summary>
		internal ExternalProxy UpstreamHttpProxy { get; set; }

		/// <summary>
		/// Gets or sets up stream HTTPS proxy.
		/// </summary>
		internal ExternalProxy UpstreamHttpsProxy { get; set; }

		/// <summary>
		/// Gets or sets the name of the host.
		/// </summary>
		internal string HostName { get; set; }

		/// <summary>
		/// Gets or sets the port.
		/// </summary>
		internal int Port { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is HTTPS.
		/// </summary>
		internal bool IsHttps { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [pre authenticate used].
		/// </summary>
		internal bool PreAuthenticateUsed { get; set; }

		/// <summary>
		/// Http version
		/// </summary>
		internal Version Version { get; set; }

		/// <summary>
		/// Gets or sets the TCP client.
		/// </summary>
		internal TcpClient TcpClient { get; set; }

		/// <summary>
		/// used to read lines from server
		/// </summary>
		internal CustomBinaryReader StreamReader { get; set; }

		/// <summary>
		/// Server stream
		/// </summary>
		internal Stream Stream { get; set; }

		/// <summary>
		/// Last time this connection was used
		/// </summary>
		internal DateTime LastAccess { get; set; }

		internal TcpConnection()
		{
			LastAccess = DateTime.Now;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Stream.Close();
			Stream.Dispose();

			TcpClient.LingerState = new LingerOption(true, 0);
			TcpClient.Client.Shutdown(SocketShutdown.Both);
			TcpClient.Client.Close();
			TcpClient.Client.Dispose();

			TcpClient.Close();
		}
	}
}