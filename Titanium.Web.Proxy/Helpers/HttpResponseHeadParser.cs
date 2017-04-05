using System;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Implements parser for the first line of a HTTP response.
	/// </summary>
	internal static class HttpResponseHeadParser
	{
		/// <summary>
		/// Parses the specified HTTP command.
		/// </summary>
		/// <param name="httpCommand">The HTTP command.</param>
		/// <returns>HttpResponseHead instance containing HTTP version, status code and status description.</returns>
		internal static HttpResponseHead Parse(string httpCommand)
		{
			if (string.IsNullOrEmpty(httpCommand))
			{
				return null;
			}

			var result = new HttpResponseHead();

			// Break up the line into three components (version, status code & status description)
			var httpCommandSplit = httpCommand.Split(ProxyConstants.SpaceSplit, 3, StringSplitOptions.RemoveEmptyEntries);

			result.Version = HttpVersionParser.Parse(httpCommandSplit, HttpCommandType.Response);

			if (result.Version == null)
			{
				return null;
			}

			if (httpCommandSplit.Length > 1 && int.TryParse(httpCommandSplit[1].Trim(), out int statusCode))
			{
				result.StatusCode = statusCode;
			}

			result.StatusDescription = httpCommandSplit.Length > 2 ? httpCommandSplit[2].Trim() : string.Empty;

			return result;
		}
	}
}