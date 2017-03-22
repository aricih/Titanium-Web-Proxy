using System.Runtime.InteropServices;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct TcpTable
	{
		public uint length;
		public TcpRow row;
	}
}