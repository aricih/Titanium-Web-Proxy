using System;
using System.Collections.Concurrent;
using System.Linq;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Authentication
{
	/// <summary>
	/// Implements cache for HTTP authorization headers.
	/// </summary>
	public static class AuthorizationHeaderCache
	{
		private static readonly ConcurrentDictionary<string, HttpHeaderCollection> CredentialCache = new ConcurrentDictionary<string, HttpHeaderCollection>();

		/// <summary>
		/// Caches the specified host name.
		/// </summary>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="authorizationHeader">The authorization header.</param>
		public static void Cache(string hostName, HttpHeader authorizationHeader)
		{
			HttpHeaderCollection headerCollection;

			if (!CredentialCache.TryGetValue(hostName, out headerCollection) || headerCollection == null)
			{
				CredentialCache.TryAdd(hostName, new HttpHeaderCollection());
			}

			CredentialCache[hostName].Add(authorizationHeader);
		}

		/// <summary>
		/// Removes the specified host name.
		/// </summary>
		/// <param name="hostName">Name of the host.</param>
		/// <returns>HttpHeaderCollection.</returns>
		public static HttpHeaderCollection Remove(string hostName)
		{
			HttpHeaderCollection removedHeaders;
			CredentialCache.TryRemove(hostName, out removedHeaders);

			return removedHeaders;
		}

		/// <summary>
		/// Determines whether the specified host name has host.
		/// </summary>
		/// <param name="hostName">Name of the host.</param>
		/// <returns><c>true</c> if the specified host name has host; otherwise, <c>false</c>.</returns>
		public static bool HasHost(string hostName)
		{
			return CredentialCache.ContainsKey(hostName);
		}

		/// <summary>
		/// Tries the get all authorization headers.
		/// </summary>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="authorizationHeaderCollection">The authorization header collection.</param>
		public static bool TryGetAllAuthorizationHeaders(string hostName, out HttpHeaderCollection authorizationHeaderCollection)
		{
			return CredentialCache.TryGetValue(hostName, out authorizationHeaderCollection);
		}

		/// <summary>
		/// Tries the get proxy authorization headers.
		/// </summary>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="authorizationHeaderCollection">The authorization header collection.</param>
		public static bool TryGetProxyAuthorizationHeaders(string hostName, out HttpHeaderCollection authorizationHeaderCollection)
		{
			HttpHeaderCollection allAuthorizationHeaders;

			var result = TryGetAllAuthorizationHeaders(hostName, out allAuthorizationHeaders);

			var proxyHeaders = allAuthorizationHeaders?.Values.Where(
					header => header.Name.StartsWith("Proxy", StringComparison.CurrentCultureIgnoreCase));

			authorizationHeaderCollection = new HttpHeaderCollection(proxyHeaders);

			return result;
		}

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public static void ClearCache()
		{
			CredentialCache.Clear();
		}
	}
}