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
	internal class TcpConnection : IDisposable
	{
		/// <summary>
		/// Gets or sets up stream HTTP proxy.
		/// </summary>
		public ExternalProxy UpstreamHttpProxy { get; set; }

		/// <summary>
		/// Gets or sets up stream HTTPS proxy.
		/// </summary>
		public ExternalProxy UpstreamHttpsProxy { get; set; }

		/// <summary>
		/// Gets or sets the name of the host.
		/// </summary>
		public string HostName { get; set; }

		/// <summary>
		/// Gets or sets the port.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is HTTPS.
		/// </summary>
		public bool IsHttps { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [pre authenticate used].
		/// </summary>
		public bool PreAuthenticateUsed { get; set; }

		/// <summary>
		/// Http version
		/// </summary>
		public Version Version { get; set; }

		/// <summary>
		/// Gets or sets the TCP client.
		/// </summary>
		public TcpClient TcpClient { get; set; }

		/// <summary>
		/// used to read lines from server
		/// </summary>
		public CustomBinaryReader StreamReader { get; set; }

		/// <summary>
		/// Server stream
		/// </summary>
		public Stream Stream { get; set; }

		/// <summary>
		/// Last time this connection was used
		/// </summary>
		public DateTime LastAccess { get; set; }

		public TcpConnection()
		{
			LastAccess = DateTime.UtcNow;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Stream?.Close();
			Stream?.Dispose();

			if (TcpClient != null)
			{
				TcpClient.LingerState = new LingerOption(true, 0);
			}

			TcpClient?.Client?.Shutdown(SocketShutdown.Both);
			TcpClient?.Client?.Close();
			TcpClient?.Client?.Dispose();

			TcpClient?.Close();

			StreamReader?.Dispose();
		}
	}
}