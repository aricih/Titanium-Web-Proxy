using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Titanium.Web.Proxy.Network
{
	/// <summary>
	/// Implements certificate generation operations.
	/// </summary>
	public class CertificateMaker
	{
		private readonly Type _typeX500Dn;

		private readonly Type _typeX509PrivateKey;

		private readonly Type _typeOid;

		private readonly Type _typeOids;

		private readonly Type _typeKuExt;

		private readonly Type _typeEkuExt;

		private readonly Type _typeRequestCert;

		private readonly Type _typeX509Extensions;

		private readonly Type _typeBasicConstraints;

		private readonly Type _typeSignerCertificate;

		private readonly Type _typeX509Enrollment;

		/// <summary>
		/// Gets the name of the type alternative.
		/// </summary>
		public Type TypeAlternativeName { get; }

		/// <summary>
		/// Gets the type alternative names.
		/// </summary>
		public Type TypeAlternativeNames { get; }

		/// <summary>
		/// Gets the type alternative names ext.
		/// </summary>
		public Type TypeAlternativeNamesExt { get; }

		private const string ProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";

		private object _sharedPrivateKey;

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateMaker"/> class.
		/// </summary>
		public CertificateMaker()
		{
			_typeX500Dn = Type.GetTypeFromProgID("X509Enrollment.CX500DistinguishedName", true);
			_typeX509PrivateKey = Type.GetTypeFromProgID("X509Enrollment.CX509PrivateKey", true);
			_typeOid = Type.GetTypeFromProgID("X509Enrollment.CObjectId", true);
			_typeOids = Type.GetTypeFromProgID("X509Enrollment.CObjectIds.1", true);
			_typeEkuExt = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionEnhancedKeyUsage");
			_typeKuExt = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionKeyUsage");
			_typeRequestCert = Type.GetTypeFromProgID("X509Enrollment.CX509CertificateRequestCertificate");
			_typeX509Extensions = Type.GetTypeFromProgID("X509Enrollment.CX509Extensions");
			_typeBasicConstraints = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionBasicConstraints");
			_typeSignerCertificate = Type.GetTypeFromProgID("X509Enrollment.CSignerCertificate");
			_typeX509Enrollment = Type.GetTypeFromProgID("X509Enrollment.CX509Enrollment");
			TypeAlternativeName = Type.GetTypeFromProgID("X509Enrollment.CAlternativeName");
			TypeAlternativeNames = Type.GetTypeFromProgID("X509Enrollment.CAlternativeNames");
			TypeAlternativeNamesExt = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionAlternativeNames");
		}

		/// <summary>
		/// Makes the certificate.
		/// </summary>
		/// <param name="sSubjectCn">The s subject cn.</param>
		/// <param name="isRoot">if set to <c>true</c> [is root].</param>
		/// <param name="signingCert">The signing cert.</param>
		/// <returns>X509Certificate2.</returns>
		public X509Certificate2 MakeCertificate(string sSubjectCn, bool isRoot, X509Certificate2 signingCert = null)
		{
			return MakeCertificateInternal(sSubjectCn, isRoot, true, signingCert);
		}

		/// <summary>
		/// Makes the certificate.
		/// </summary>
		/// <param name="isRoot">if set to <c>true</c> [is root].</param>
		/// <param name="fullSubject">The full subject.</param>
		/// <param name="privateKeyLength">Length of the private key.</param>
		/// <param name="hashAlg">The hash alg.</param>
		/// <param name="validFrom">The valid from.</param>
		/// <param name="validTo">The valid to.</param>
		/// <param name="signingCertificate">The signing certificate.</param>
		/// <returns>X509Certificate2.</returns>
		/// <exception cref="ArgumentException">You must specify a Signing Certificate if and only if you are not creating a root.;oSigningCertificate</exception>
		private X509Certificate2 MakeCertificate(bool isRoot, string fullSubject, int privateKeyLength, string hashAlg, DateTime validFrom, DateTime validTo, X509Certificate2 signingCertificate)
		{
			if (isRoot != (null == signingCertificate))
			{
				throw new ArgumentException("You must specify a Signing Certificate if and only if you are not creating a root.", nameof(signingCertificate));
			}

			var x500Dn = Activator.CreateInstance(_typeX500Dn);
			var subject = new object[] { fullSubject, 0 };

			_typeX500Dn.InvokeMember("Encode", BindingFlags.InvokeMethod, null, x500Dn, subject);

			var x500Dn2 = Activator.CreateInstance(_typeX500Dn);

			if (!isRoot)
			{
				subject[0] = signingCertificate.Subject;
			}

			_typeX500Dn.InvokeMember("Encode", BindingFlags.InvokeMethod, null, x500Dn2, subject);

			object sharedPrivateKey = null;

			if (!isRoot)
			{
				sharedPrivateKey = _sharedPrivateKey;
			}

			if (sharedPrivateKey == null)
			{
				sharedPrivateKey = Activator.CreateInstance(_typeX509PrivateKey);
				subject = new object[] { ProviderName };
				_typeX509PrivateKey.InvokeMember("ProviderName", BindingFlags.PutDispProperty, null, sharedPrivateKey, subject);
				subject[0] = 2;
				_typeX509PrivateKey.InvokeMember("ExportPolicy", BindingFlags.PutDispProperty, null, sharedPrivateKey, subject);
				subject = new object[] { (isRoot ? 2 : 1) };
				_typeX509PrivateKey.InvokeMember("KeySpec", BindingFlags.PutDispProperty, null, sharedPrivateKey, subject);

				if (!isRoot)
				{
					subject = new object[] { 176 };
					_typeX509PrivateKey.InvokeMember("KeyUsage", BindingFlags.PutDispProperty, null, sharedPrivateKey, subject);
				}

				subject[0] = privateKeyLength;
				_typeX509PrivateKey.InvokeMember("Length", BindingFlags.PutDispProperty, null, sharedPrivateKey, subject);
				_typeX509PrivateKey.InvokeMember("Create", BindingFlags.InvokeMethod, null, sharedPrivateKey, null);

				if (!isRoot)
				{
					_sharedPrivateKey = sharedPrivateKey;
				}
			}

			subject = new object[1];
			var obj3 = Activator.CreateInstance(_typeOid);
			subject[0] = "1.3.6.1.5.5.7.3.1";
			_typeOid.InvokeMember("InitializeFromValue", BindingFlags.InvokeMethod, null, obj3, subject);
			var obj4 = Activator.CreateInstance(_typeOids);
			subject[0] = obj3;
			_typeOids.InvokeMember("Add", BindingFlags.InvokeMethod, null, obj4, subject);
			var obj5 = Activator.CreateInstance(_typeEkuExt);
			subject[0] = obj4;
			_typeEkuExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, obj5, subject);
			var obj6 = Activator.CreateInstance(_typeRequestCert);
			subject = new[] { 1, sharedPrivateKey, string.Empty };
			_typeRequestCert.InvokeMember("InitializeFromPrivateKey", BindingFlags.InvokeMethod, null, obj6, subject);
			subject = new[] { x500Dn };
			_typeRequestCert.InvokeMember("Subject", BindingFlags.PutDispProperty, null, obj6, subject);
			subject[0] = x500Dn;
			_typeRequestCert.InvokeMember("Issuer", BindingFlags.PutDispProperty, null, obj6, subject);
			subject[0] = validFrom;
			_typeRequestCert.InvokeMember("NotBefore", BindingFlags.PutDispProperty, null, obj6, subject);
			subject[0] = validTo;
			_typeRequestCert.InvokeMember("NotAfter", BindingFlags.PutDispProperty, null, obj6, subject);
			var obj7 = Activator.CreateInstance(_typeKuExt);
			subject[0] = 176;
			_typeKuExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, obj7, subject);
			var obj8 = _typeRequestCert.InvokeMember("X509Extensions", BindingFlags.GetProperty, null, obj6, null);
			subject = new object[1];

			if (!isRoot)
			{
				subject[0] = obj7;
				_typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, obj8, subject);
			}
			subject[0] = obj5;
			_typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, obj8, subject);

			if (!isRoot)
			{
				var obj12 = Activator.CreateInstance(_typeSignerCertificate);
				subject = new object[] { 0, 0, 12, signingCertificate.Thumbprint };
				_typeSignerCertificate.InvokeMember("Initialize", BindingFlags.InvokeMethod, null, obj12, subject);
				subject = new[] { obj12 };
				_typeRequestCert.InvokeMember("SignerCertificate", BindingFlags.PutDispProperty, null, obj6, subject);
			}
			else
			{
				var obj13 = Activator.CreateInstance(_typeBasicConstraints);
				subject = new object[] { "true", "0" };
				_typeBasicConstraints.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, obj13, subject);
				subject = new[] { obj13 };
				_typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, obj8, subject);
			}

			var obj14 = Activator.CreateInstance(_typeOid);
			subject = new object[] { 1, 0, 0, hashAlg };
			_typeOid.InvokeMember("InitializeFromAlgorithmName", BindingFlags.InvokeMethod, null, obj14, subject);
			subject = new[] { obj14 };
			_typeRequestCert.InvokeMember("HashAlgorithm", BindingFlags.PutDispProperty, null, obj6, subject);
			_typeRequestCert.InvokeMember("Encode", BindingFlags.InvokeMethod, null, obj6, null);
			var obj15 = Activator.CreateInstance(_typeX509Enrollment);
			subject[0] = obj6;
			_typeX509Enrollment.InvokeMember("InitializeFromRequest", BindingFlags.InvokeMethod, null, obj15, subject);

			if (isRoot)
			{
				subject[0] = ProxyServer.Instance?.RootCertificateFriendlyName ??
							 ProxyServer.DefaultRootCertificateFriendlyName;
				_typeX509Enrollment.InvokeMember("CertificateFriendlyName", BindingFlags.PutDispProperty, null, obj15, subject);
			}

			subject[0] = 0;
			var obj16 = _typeX509Enrollment.InvokeMember("CreateRequest", BindingFlags.InvokeMethod, null, obj15, subject);
			subject = new[] { 2, obj16, 0, string.Empty };
			_typeX509Enrollment.InvokeMember("InstallResponse", BindingFlags.InvokeMethod, null, obj15, subject);
			subject = new object[] { null, 0, 1 };

			try
			{
				var empty = (string)_typeX509Enrollment.InvokeMember("CreatePFX", BindingFlags.InvokeMethod, null, obj15, subject);
				return new X509Certificate2(Convert.FromBase64String(empty), string.Empty, X509KeyStorageFlags.Exportable);
			}
			// Ignore any exception and return null
			catch (Exception)
			{
			}

			return null;
		}

		/// <summary>
		/// Makes the certificate internal.
		/// </summary>
		/// <param name="sSubjectCn">The s subject cn.</param>
		/// <param name="isRoot">if set to <c>true</c> [is root].</param>
		/// <param name="switchToMtaIfNeeded">if set to <c>true</c> [switch to MTA if needed].</param>
		/// <param name="signingCert">The signing cert.</param>
		/// <returns>X509Certificate2.</returns>
		private X509Certificate2 MakeCertificateInternal(string sSubjectCn, bool isRoot, bool switchToMtaIfNeeded, X509Certificate2 signingCert = null)
		{
			X509Certificate2 rCert = null;

			if (switchToMtaIfNeeded && Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
			{
				var manualResetEvent = new ManualResetEvent(false);
				ThreadPool.QueueUserWorkItem(o =>
				{
					rCert = MakeCertificateInternal(sSubjectCn, isRoot, false, signingCert);
					manualResetEvent.Set();
				});
				manualResetEvent.WaitOne();
				manualResetEvent.Close();
				return rCert;
			}

			var fullSubject = $"CN={sSubjectCn}";//Subject
			const string hashAlgo = "SHA256"; //Sig Algo
			const int graceDays = -366; //Grace Days
			const int validDays = 1825; //ValiDays
			const int keyLength = 2048; //KeyLength

			var graceTime = DateTime.Now.AddDays(graceDays);
			var now = DateTime.Now;

			rCert = !isRoot ? MakeCertificate(false, fullSubject, keyLength, hashAlgo, graceTime, now.AddDays(validDays), signingCert) : MakeCertificate(true, fullSubject, keyLength, hashAlgo, graceTime, now.AddDays(validDays), null);

			return rCert;
		}
	}

}
