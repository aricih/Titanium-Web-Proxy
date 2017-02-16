using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Extensions;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using Titanium.Web.Proxy.Tcp;

namespace Titanium.Web.Proxy.Helpers
{
	internal enum IpVersion
	{
		Ipv4 = 1,
		Ipv6 = 2,
	}

	internal partial class NativeMethods
	{
		internal const int AfInet = 2;
		internal const int AfInet6 = 23;

		internal enum TcpTableType
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

		/// <summary>
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct TcpTable
		{
			public uint length;
			public TcpRow row;
		}

		/// <summary>
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa366913.aspx"/>
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct TcpRow
		{
			public TcpState state;
			public uint localAddr;
			public byte localPort1;
			public byte localPort2;
			public byte localPort3;
			public byte localPort4;
			public uint remoteAddr;
			public byte remotePort1;
			public byte remotePort2;
			public byte remotePort3;
			public byte remotePort4;
			public int owningPid;
		}

		/// <summary>
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa365928.aspx"/>
		/// </summary>
		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int size, bool sort, int ipVersion, int tableClass, int reserved);
	}

	internal class TcpHelper
	{
		/// <summary>
		/// Gets the extended TCP table.
		/// </summary>
		/// <returns>Collection of <see cref="TcpRow"/>.</returns>
		internal static TcpTable GetExtendedTcpTable(IpVersion ipVersion)
		{
			var tcpRows = new List<TcpRow>();

			var tcpTable = IntPtr.Zero;
			var tcpTableLength = 0;

			var ipVersionValue = ipVersion == IpVersion.Ipv4 ? NativeMethods.AfInet : NativeMethods.AfInet6;

			if (NativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, false, ipVersionValue, (int)NativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
			{
				try
				{
					tcpTable = Marshal.AllocHGlobal(tcpTableLength);
					if (NativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, ipVersionValue, (int)NativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
					{
						NativeMethods.TcpTable table = (NativeMethods.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(NativeMethods.TcpTable));

						IntPtr rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));

						for (int i = 0; i < table.length; ++i)
						{
							tcpRows.Add(new TcpRow((NativeMethods.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(NativeMethods.TcpRow))));
							rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(NativeMethods.TcpRow)));
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

			return new TcpTable(tcpRows);
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
		internal static async Task SendRaw(int bufferSize, int connectionTimeOutSeconds,
			Uri requestUri, string httpCmd, Version httpVersion, Dictionary<string, HttpHeader> requestHeaders,
			bool isHttps, SslProtocols supportedProtocols,
			RemoteCertificateValidationCallback remoteCertificateValidationCallback, LocalCertificateSelectionCallback localCertificateSelectionCallback,
			Stream clientStream, TcpConnectionFactory tcpConnectionFactory, CancellationToken cancellationToken = default(CancellationToken))
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

			var tcpConnection = await tcpConnectionFactory.CreateClient(bufferSize, connectionTimeOutSeconds,
										requestUri,
										requestHeaders,
										httpVersion, isHttps,
										supportedProtocols, remoteCertificateValidationCallback, localCertificateSelectionCallback,
										null, null, clientStream, cancellationToken: cancellationToken);

			try
			{
				var tunnelStream = tcpConnection.Stream;

				//Now async relay all server=>client & client=>server data
				var sendRelay = clientStream.CopyToAsync(sb?.ToString() ?? string.Empty, tunnelStream, cancellationToken: cancellationToken);

				var receiveRelay = tunnelStream.CopyToAsync(string.Empty, clientStream, cancellationToken: cancellationToken);

				await Task.WhenAll(sendRelay, receiveRelay);
			}
			finally
			{
				tcpConnection.Dispose();
			}
		}
	}
}