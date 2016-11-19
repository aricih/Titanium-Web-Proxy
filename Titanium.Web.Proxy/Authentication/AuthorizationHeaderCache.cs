using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Authentication
{
	public static class AuthorizationHeaderCache
	{
		private static readonly Lazy<ConcurrentDictionary<string, HttpHeaderCollection>> CredentialCache = new Lazy<ConcurrentDictionary<string, HttpHeaderCollection>>(() => new ConcurrentDictionary<string, HttpHeaderCollection>());

		public static void Cache(string hostName, HttpHeader authorizationHeader)
		{
			HttpHeaderCollection headerCollection;

			if (!CredentialCache.Value.TryGetValue(hostName, out headerCollection) || headerCollection == null)
			{
				CredentialCache.Value[hostName] = new HttpHeaderCollection();
			}

			CredentialCache.Value[hostName].Add(authorizationHeader);
		}

		public static HttpHeaderCollection Remove(string hostName)
		{
			HttpHeaderCollection removedHeaders;
			CredentialCache.Value.TryRemove(hostName, out removedHeaders);

			return removedHeaders;
		}

		public static bool HasHost(string hostName)
		{
			return CredentialCache.Value.ContainsKey(hostName);
		}

		public static bool TryGetAllAuthorizationHeaders(string hostName, out HttpHeaderCollection authorizationHeaderCollection)
		{
			return CredentialCache.Value.TryGetValue(hostName, out authorizationHeaderCollection);
		}

		public static bool TryGetProxyAuthorizationHeaders(string hostName, out HttpHeaderCollection authorizationHeaderCollection)
		{
			HttpHeaderCollection allAuthorizationHeaders;

			var result = TryGetAllAuthorizationHeaders(hostName, out allAuthorizationHeaders);

			var proxyHeaders = allAuthorizationHeaders?.Values.Where(
					header => header.Name.StartsWith("Proxy", StringComparison.CurrentCultureIgnoreCase));

			authorizationHeaderCollection = new HttpHeaderCollection(proxyHeaders);

			return result;
		}

		public static void ClearCache()
		{
			CredentialCache.Value.Clear();
		}
	}
}