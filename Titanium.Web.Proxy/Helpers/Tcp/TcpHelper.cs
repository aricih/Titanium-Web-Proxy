using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Extensions;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;

namespace Titanium.Web.Proxy.Helpers
{
	internal class TcpHelper
	{
		/// <summary>
		/// Gets the extended TCP table.
		/// </summary>
		/// <returns>Collection of <see cref="TcpRow"/>.</returns>
		internal static Tcp.TcpTable GetExtendedTcpTable(IpVersion ipVersion)
		{
			var tcpRows = new List<Tcp.TcpRow>();

			var tcpTable = IntPtr.Zero;
			var tcpTableLength = 0;

			var ipVersionValue = ipVersion == IpVersion.Ipv4 ? NativeMethods.AfInet : NativeMethods.AfInet6;

			if (NativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, false, ipVersionValue, (int)TcpTableType.OwnerPidAll, 0) != 0)
			{
				try
				{
					tcpTable = Marshal.AllocHGlobal(tcpTableLength);
					if (NativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, ipVersionValue, (int)TcpTableType.OwnerPidAll, 0) == 0)
					{
						var table = (TcpTable)Marshal.PtrToStructure(tcpTable, typeof(TcpTable));

						var rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));

						for (var i = 0; i < table.length; ++i)
						{
							tcpRows.Add(new Tcp.TcpRow((TcpRow)Marshal.PtrToStructure(rowPtr, typeof(TcpRow))));
							rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(TcpRow)));
						}
					}
				}
				finally
				{
					if (tcpTable != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(tcpTable);
					}
				}
			}

			return new Tcp.TcpTable(tcpRows);
		}

		/// <summary>
		/// Relays the input clientStream to the server at the specified host name and port with the given httpCmd and headers as prefix
		/// Usefull for websocket requests
		/// </summary>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="connectionTimeOutSeconds">The connection time out seconds.</param>
		/// <param name="requestUri">The request URI.</param>
		/// <param name="httpCmd">The HTTP command.</param>
		/// <param name="httpVersion">The HTTP version.</param>
		/// <param name="requestHeaders">The request headers.</param>
		/// <param name="isHttps">if set to <c>true</c> [is HTTPS].</param>
		/// <param name="supportedProtocols">The supported protocols.</param>
		/// <param name="remoteCertificateValidationCallback">The remote certificate validation callback.</param>
		/// <param name="localCertificateSelectionCallback">The local certificate selection callback.</param>
		/// <param name="clientStream">The client stream.</param>
		/// <param name="tcpConnectionFactory">The TCP connection factory.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		internal static async Task SendRaw(int bufferSize,
			int connectionTimeOutSeconds,
			Uri requestUri,
			string httpCmd,
			Version httpVersion,
			Dictionary<string, HttpHeader> requestHeaders,
			bool isHttps,
			SslProtocols supportedProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback,
			LocalCertificateSelectionCallback localCertificateSelectionCallback,
			Stream clientStream,
			TcpConnectionFactory tcpConnectionFactory,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			// Prepare the prefix content
			StringBuilder sb = null;
			if (httpCmd != null || requestHeaders != null)
			{
				sb = new StringBuilder();

				if (httpCmd != null)
				{
					sb.Append(httpCmd);
					sb.Append(Environment.NewLine);
				}

				if (requestHeaders != null)
				{
					foreach (var header in requestHeaders.Select(t => t.Value.ToString()))
					{
						sb.Append(header);
						sb.Append(Environment.NewLine);
					}
				}

				sb.Append(Environment.NewLine);
			}

			var tcpConnection = await tcpConnectionFactory.CreateClient(bufferSize,
				connectionTimeOutSeconds,
				requestUri,
				requestHeaders,
				httpVersion,
				isHttps,
				supportedProtocols,
				remoteCertificateValidationCallback,
				localCertificateSelectionCallback,
				null,
				null,
				clientStream,
				cancellationToken: cancellationToken);

			try
			{
				var tunnelStream = tcpConnection.Stream;

				//Now async relay all server=>client & client=>server data
				var sendRelay = clientStream.CopyToAsync(sb?.ToString() ?? String.Empty, tunnelStream, cancellationToken: cancellationToken);

				var receiveRelay = tunnelStream.CopyToAsync(String.Empty, clientStream, cancellationToken: cancellationToken);

				await Task.WhenAny(
					Task.WhenAll(sendRelay, receiveRelay),
					Task.Delay(ProxyServer.TaskTimeout, cancellationToken: cancellationToken));
			}
			finally
			{
				tcpConnection.Dispose();
			}
		}

		/// <summary>
		/// Native TCP table type enumeration
		/// </summary>
		private enum TcpTableType
		{
			BasicListener,
			BasicConnections,
			BasicAll,
			OwnerPidListener,
			OwnerPidConnections,
			OwnerPidAll,
			OwnerModuleListener,
			OwnerModuleConnections,
			OwnerModuleAll,
		}
	}
}