using System;

namespace Titanium.Web.Proxy.Helpers
{
	internal static class HttpVersionParser
	{
		private static readonly Version HttpVersion10 = new Version(1, 0);
		private static readonly Version HttpVersion11 = new Version(1, 1);

		private const string HttpVersion10String = "http/1.0";

		internal static Version Parse(string[] httpCommandSplit, HttpCommandType commandType)
		{
			var version = HttpVersion11;

			var versionIndex = commandType == HttpCommandType.Response ? 0 : 2;

			if (httpCommandSplit.Length > versionIndex && httpCommandSplit[versionIndex].Equals(HttpVersion10String, StringComparison.InvariantCultureIgnoreCase))
			{
				version = HttpVersion10;
			}

			return version;
		}
	}

	/// <summary>
	/// Enumerates Http command types
	/// </summary>
	internal enum HttpCommandType
	{
		Response = 0,
		Request = 1
	}
}