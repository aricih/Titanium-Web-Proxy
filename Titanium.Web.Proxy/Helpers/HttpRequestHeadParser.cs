using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Implements parser for the first line of a HTTP request.
	/// </summary>
	internal static class HttpRequestHeadParser
	{
		/// <summary>
		/// Parses the specified HTTP command.
		/// </summary>
		/// <param name="httpCommand">The HTTP command.</param>
		/// <returns>HttpRequestHead instance containing request method, URL, HTTP version info.</returns>
		internal static HttpRequestHead Parse(string httpCommand)
		{
			var result = new HttpRequestHead();

			// Break up the line into three components (method, remote URL & Http Version)
			var httpCommandSplit = httpCommand.Split(ProxyConstants.SpaceSplit, 3);

			result.Method = httpCommandSplit.Length > 0 ? httpCommandSplit[0].Trim() : string.Empty;
			result.Url = httpCommandSplit.Length > 1 ? httpCommandSplit[1].Trim() : string.Empty;
			result.Version = HttpVersionParser.Parse(httpCommandSplit, HttpCommandType.Request);

			return result;
		} 
	}
}