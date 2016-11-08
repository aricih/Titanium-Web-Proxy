using System;
using System.Collections.Generic;
using System.Linq;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Authentication
{
	public static class AuthorizationHeaderCache
	{
		private static readonly Lazy<Dictionary<string, HttpHeaderCollection>> CredentialCache = new Lazy<Dictionary<string, HttpHeaderCollection>>(() => new Dictionary<string, HttpHeaderCollection>());

		public static void Cache(string hostName, HttpHeader authorizationHeader)
		{
			HttpHeaderCollection headerCollection;

			if (!CredentialCache.Value.TryGetValue(hostName, out headerCollection) || headerCollection == null)
			{
				CredentialCache.Value[hostName] = new HttpHeaderCollection();
			}

			CredentialCache.Value[hostName].Add(authorizationHeader);
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