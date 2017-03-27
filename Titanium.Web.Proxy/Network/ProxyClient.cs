using System;
using System.IO;
using System.Net.Sockets;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// This class wraps Tcp connection to client
	/// </summary>
	public class ProxyClient : IDisposable
	{
		private readonly object _disposeLock = new object();

		private bool _disposed;

		/// <summary>
		/// TcpClient used to communicate with client
		/// </summary>
		internal TcpClient TcpClient { get; set; }

		/// <summary>
		/// holds the stream to client
		/// </summary>
		internal Stream ClientStream { get; set; }

		/// <summary>
		/// Used to read line by line from client
		/// </summary>
		internal CustomBinaryReader ClientStreamReader { get; set; }

		/// <summary>
		/// used to write line by line to client
		/// </summary>
		internal StreamWriter ClientStreamWriter { get; set; }

		protected virtual void Dispose(bool disposing)
		{
			lock (_disposeLock)
			{
				if (!disposing || _disposed)
				{
					return;
				}

				TcpClient?.Dispose();
				ClientStream?.Dispose();
				ClientStreamReader?.Dispose();
				ClientStreamWriter?.Dispose();
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
