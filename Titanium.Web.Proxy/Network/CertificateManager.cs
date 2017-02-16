using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// A class to manage SSL certificates used by this proxy server
	/// </summary>
	internal class CertificateManager : IDisposable
	{
		private readonly CertificateMaker _certEngine;

		private readonly ICertificateCache _certificateCache;

		private readonly Action<Exception> _exceptionFunc;

		/// <summary>
		/// Gets or sets a value indicating whether [clear certificates].
		/// </summary>
		private bool ClearCertificates { get; set; }

		/// <summary>
		/// Gets the issuer.
		/// </summary>
		internal string Issuer { get; private set; }

		/// <summary>
		/// Gets the name of the root certificate.
		/// </summary>
		internal string RootCertificateName { get; }

		/// <summary>
		/// Gets or sets the root certificate.
		/// </summary>
		internal X509Certificate2 RootCertificate { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateManager"/> class.
		/// </summary>
		/// <param name="issuer">The issuer.</param>
		/// <param name="rootCertificateName">Name of the root certificate.</param>
		/// <param name="exceptionFunc">The exception function.</param>
		internal CertificateManager(string issuer, string rootCertificateName, Action<Exception> exceptionFunc)
		{
			_exceptionFunc = exceptionFunc;

			_certEngine = new CertificateMaker();

			Issuer = issuer;
			RootCertificateName = rootCertificateName;

			_certificateCache = new CertificateCache();
		}

		/// <summary>
		/// Gets the root certificate.
		/// </summary>
		/// <returns>X509Certificate2.</returns>
		internal X509Certificate2 GetRootCertificate()
		{
			var fileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "rootCert.pfx");

			if (!File.Exists(fileName))
			{
				return null;
			}

			try
			{
				return new X509Certificate2(fileName, string.Empty, X509KeyStorageFlags.Exportable);
			}
			catch (Exception e)
			{
				_exceptionFunc(e);
			}

			return null;
		}

		/// <summary>
		/// Attempts to create a RootCertificate
		/// </summary>
		/// <returns>true if succeeded, else false</returns>
		internal bool CreateTrustedRootCertificate()
		{

			RootCertificate = GetRootCertificate();

			if (RootCertificate != null)
			{
				return true;
			}

			try
			{
				RootCertificate = CreateCertificate(RootCertificateName, true);
			}
			catch (Exception e)
			{
				_exceptionFunc(e);
			}

			if (RootCertificate == null)
			{
				return RootCertificate != null;
			}

			try
			{
				var fileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "rootCert.pfx");
				File.WriteAllBytes(fileName, RootCertificate.Export(X509ContentType.Pkcs12));
			}
			catch (Exception e)
			{
				_exceptionFunc(e);
			}

			return RootCertificate != null;
		}

		/// <summary>
		/// Create an SSL certificate
		/// </summary>
		/// <param name="certificateName">Name of the certificate.</param>
		/// <param name="isRootCertificate">if set to <c>true</c> [is root certificate].</param>
		/// <returns>X509Certificate2.</returns>
		internal virtual X509Certificate2 CreateCertificate(string certificateName, bool isRootCertificate)
		{
			try
			{
				if (_certificateCache.ContainsKey(certificateName))
				{
					var cached = _certificateCache[certificateName];
					cached.LastAccess = DateTime.Now;
					return cached.Certificate;
				}
			}
			catch
			{

			}

			X509Certificate2 certificate = null;

			lock (string.Intern(certificateName))
			{
				if (!_certificateCache.ContainsKey(certificateName))
				{
					try
					{
						certificate = _certEngine.MakeCertificate(certificateName, isRootCertificate, RootCertificate);
					}
					catch (Exception e)
					{
						_exceptionFunc(e);
					}
					if (certificate != null && !_certificateCache.ContainsKey(certificateName))
					{
						_certificateCache.Add(certificateName, new CachedCertificate { Certificate = certificate });
					}
				}
				else
				{
					if (!_certificateCache.ContainsKey(certificateName))
					{
						return certificate;
					}

					var cached = _certificateCache[certificateName];
					cached.LastAccess = DateTime.Now;
					return cached.Certificate;
				}
			}

			return certificate;
		}

		/// <summary>
		/// Stops the certificate cache clear process
		/// </summary>
		internal void StopClearIdleCertificates()
		{
			ClearCertificates = false;
		}

		/// <summary>
		/// A method to clear outdated certificates
		/// </summary>
		internal async void ClearIdleCertificates(int certificateCacheTimeOutMinutes)
		{
			ClearCertificates = true;
			while (ClearCertificates)
			{
				var cutOff = DateTime.Now.AddMinutes(-1 * certificateCacheTimeOutMinutes);

				var outdated = _certificateCache
					.Where(x => x.Value.LastAccess < cutOff)
					.ToList();

				foreach (var cache in outdated)
					_certificateCache.Remove(cache.Key);

				//after a minute come back to check for outdated certificates in cache
				await Task.Delay(1000 * 60);
			}
		}

		internal bool TrustRootCertificate()
		{
			if (RootCertificate == null)
			{
				return false;
			}
			try
			{
				var x509RootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
				var x509PersonalStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);

				x509RootStore.Open(OpenFlags.ReadWrite);
				x509PersonalStore.Open(OpenFlags.ReadWrite);

				try
				{
					if (!x509RootStore.Certificates.Contains(RootCertificate))
					{
						x509RootStore.Add(RootCertificate);
					}

					if (!x509PersonalStore.Certificates.Contains(RootCertificate))
					{
						x509PersonalStore.Add(RootCertificate);
					}
				}
				finally
				{
					x509RootStore.Close();
					x509PersonalStore.Close();
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void Dispose()
		{
		}
	}
}