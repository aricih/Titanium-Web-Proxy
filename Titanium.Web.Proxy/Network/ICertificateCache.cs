using System.Collections.Generic;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// Alias interface for concurrent dictionary to cache certificates
	/// </summary>
	/// <seealso cref="System.Collections.Generic.IDictionary{System.String, Titanium.Web.Proxy.Network.CachedCertificate}" />
	internal interface ICertificateCache : IDictionary<string, CachedCertificate>
	{
		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.
		/// </summary>
		/// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
		/// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the key; otherwise, false.</returns>
		new bool ContainsKey(string key);

		/// <summary>
		/// Caches the specified certificate.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="certificate">The certificate.</param>
		new void Add(string key, CachedCertificate certificate);

		/// <summary>
		/// Gets or sets the <see cref="CachedCertificate"/> with the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>CachedCertificate instance.</returns>
		new CachedCertificate this[string key] { get; set; }
	}
}