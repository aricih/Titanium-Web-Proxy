using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Titanium.Web.Proxy.Helpers
{
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
}