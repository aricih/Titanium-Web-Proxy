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
		private const string WorldWideWebPrefix = "www.";

		/// <summary>
		/// Gets the common key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Common key for the cache entry.</returns>
		private string GetCommonKey(string key)
		{
			var commonKey = Keys.FirstOrDefault(key.EndsWith);

			if (!string.IsNullOrEmpty(commonKey))
			{
				return commonKey;
			}

			commonKey = Keys.FirstOrDefault(existingKey => existingKey.EndsWith(key));

			return !string.IsNullOrEmpty(commonKey) ? commonKey : key;
		}

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
			if (key.StartsWith(WorldWideWebPrefix, StringComparison.InvariantCultureIgnoreCase))
			{
				key = key.Remove(0, WorldWideWebPrefix.Length);
			}

			TryAdd(key, certificate);
		}

		/// <summary>
		/// Gets or sets the <see cref="CachedCertificate"/> with the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>CachedCertificate instance.</returns>
		public new CachedCertificate this[string key]
		{
			get
			{
				return base[GetCommonKey(key)];
			}
			set
			{
				base[GetCommonKey(key)] = value;
			}
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