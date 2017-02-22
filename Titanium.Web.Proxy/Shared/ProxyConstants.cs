using System;
using System.Text;

namespace Titanium.Web.Proxy.Shared
{
	/// <summary>
	/// Literals shared by Proxy Server
	/// </summary>
	internal class ProxyConstants
	{
		internal static readonly string CoreNewLine = "\r\n";
		internal static readonly char[] SpaceSplit = { ' ' };
		internal static readonly char[] ColonSplit = { ':' };
		internal static readonly char[] SemiColonSplit = { ';' };

		internal static readonly Encoding DefaultEncoding = Encoding.UTF8;

		internal static readonly byte[] NewLineBytes = DefaultEncoding.GetBytes(CoreNewLine);

		internal static readonly byte[] ChunkEnd = DefaultEncoding.GetBytes($"00{CoreNewLine}{CoreNewLine}");

	}
}