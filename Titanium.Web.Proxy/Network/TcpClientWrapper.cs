using System.IO;
using System.Net.Sockets;

namespace Titanium.Web.Proxy.Network
{
	internal class TcpClientWrapper
	{
		private TcpClient _client;

		/// <summary>
		/// Gets or sets the client.
		/// </summary>
		public TcpClient Client
		{
			get { return _client; }
			set
			{
				_client = value;
				Stream = _client.GetStream();
			}
		}

		/// <summary>
		/// Gets or sets the stream.
		/// </summary>
		public Stream Stream { get; internal set; }

		/// <summary>
		/// Gets or sets a value indicating whether [pre authenticate used].
		/// </summary>
		internal bool IsPreAuthenticateUsed { get; set; }
	}
}