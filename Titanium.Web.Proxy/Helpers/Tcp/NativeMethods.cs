using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Encapsulates native method calls
	/// </summary>
	internal class NativeMethods
	{
		internal const int AfInet = 2;
		internal const int AfInet6 = 23;

		/// <summary>
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa365928.aspx"/>
		/// </summary>
		[DllImport("iphlpapi.dll", SetLastError = true)]
		internal static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int size, bool sort, int ipVersion, int tableClass, int reserved);

		[DllImport("wininet.dll")]
		internal static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer,
			int dwBufferLength);
	}
}