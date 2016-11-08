using System.IO;
using System.Net.Sockets;

namespace Titanium.Web.Proxy.Network
{
	internal class TcpClientWrapper
	{
		private TcpClient client;

		public TcpClient Client
		{
			get { return client; }
			set
			{
				client = value;
				Stream = client.GetStream();
			}
		}

		public Stream Stream { get; internal set; }
	}
}