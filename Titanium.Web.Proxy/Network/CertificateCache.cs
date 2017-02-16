using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// Custom certificate cache.
	/// </summary>
	/// <seealso cref="System.Collections.Concurrent.ConcurrentDictionary{System.String, Titanium.Web.Proxy.Network.CachedCertificate}" />
	/// <seealso cref="Titanium.Web.Proxy.Network.ICertificateCache" />
	internal class CertificateCache : ConcurrentDictionary<string, CachedCertificate>, ICertificateCache
	{
		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
		/// <returns>true if the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
		private new bool ContainsKey(string key)
		{
			return base.ContainsKey(key)
				   || Keys.FirstOrDefault(key.EndsWith) != null
				   || Keys.FirstOrDefault(existingKey => existingKey.EndsWith(key)) != null;
		}

		/// <summary>
		/// Caches the specified certificate.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="certificate">The certificate.</param>
		private void Add(string key, CachedCertificate certificate)
		{
			if (key.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase))
			{
				key = key.Remove(0, 4);
			}

			TryAdd(key, certificate);
		}

		/// <summary>
		/// Caches the specified certificate.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="certificate">The certificate.</param>
		void IDictionary<string, CachedCertificate>.Add(string key, CachedCertificate certificate)
		{
			Add(key, certificate);
		}

		/// <summary>
		/// Caches the specified certificate.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="certificate">The certificate.</param>
		void ICertificateCache.Add(string key, CachedCertificate certificate)
		{
			Add(key, certificate);
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
		/// <returns>true if the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
		bool IDictionary<string, CachedCertificate>.ContainsKey(string key)
		{
			return ContainsKey(key);
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
		/// <returns>true if the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
		bool ICertificateCache.ContainsKey(string key)
		{
			return ContainsKey(key);
		}
	}
}