using System;

namespace Titanium.Web.Proxy.Helpers
{
	internal static class HttpVersionParser
	{
		private static readonly Version HttpVersion10 = new Version(1, 0);
		private static readonly Version HttpVersion11 = new Version(1, 1);

		private const string HttpVersion10String = "http/1.0";
		private const string HttpVersion11String = "http/1.1";

		internal static Version Parse(string[] httpCommandSplit, HttpCommandType commandType)
		{
			var versionIndex = commandType == HttpCommandType.Response ? 0 : 2;

			var versionString = httpCommandSplit.Length > versionIndex ? httpCommandSplit[versionIndex] : null;

			if (string.IsNullOrEmpty(versionString))
			{
				return null;
			}

			if (versionString.Equals(HttpVersion10String, StringComparison.InvariantCultureIgnoreCase))
			{
				return HttpVersion10;
			}

			return versionString.Equals(HttpVersion11String, StringComparison.InvariantCultureIgnoreCase) ? HttpVersion11 : null;
		}
	}
}