using NUnit.Framework;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.UnitTests.Http
{
	[TestFixture]
	public class RequestFixture
	{
		private const string None = "None";

		private Request _request;

		[SetUp]
		public void SetUp()
		{
			_request = new Request();
		}

		[TestCase(None, ExpectedResult = null, TestName = "Works properly without Content-Type header")]
		[TestCase(null, ExpectedResult = null, TestName = "Handles null value as Content-Type")]
		[TestCase("", ExpectedResult = "", TestName = "Handles empty value as Content-Type")]
		[TestCase("text/html; charset=utf-8", ExpectedResult = "text/html; charset=utf-8", TestName = "Handles any proper value")]
		public string ContentType_get_works_properly(string contentTypeHeaderValue)
		{
			if (contentTypeHeaderValue != None)
			{
				_request.Headers["Content-Type"] = new HttpHeader("Content-Type", contentTypeHeaderValue);
			}

			return _request.ContentType;
		}

		[TestCase(null, TestName = "Handles null input")]
		[TestCase("", TestName = "Handles empty input")]
		[TestCase("text/html; charset=utf-8", TestName = "Handles any proper input")]
		[TestCase(null, true, TestName = "Handles null input when the same header is introduced previously")]
		[TestCase("", true, TestName = "Handles empty input when the same header is introduced previously")]
		[TestCase("text/html; charset=utf-8", true, TestName = "Handles any proper input when the same header is introduced previously")]
		public void ContentType_set_works_properly(string contentTypeHeaderValue, bool previouslyContainsHeader = false)
		{
			if (previouslyContainsHeader)
			{
				_request.ContentType = "some other content type that should be overwritten";
			}

			_request.ContentType = contentTypeHeaderValue;

			// Does OrdinalIgnoreCase string comparer works properly; Content-Type -> content-type
			Assert.AreEqual(_request.Headers.ContainsKey("content-type"), true);

			// Does setter works properly
			Assert.AreEqual(_request.Headers["Content-Type"].Value, contentTypeHeaderValue);
		}

		[TestCase(None, ExpectedResult = -1, TestName = "Works properly without Content-Length header")]
		[TestCase(null, ExpectedResult = 0, TestName = "Handles null value as Content-Length")]
		[TestCase("", ExpectedResult = 0, TestName = "Handles empty value as Content-Length")]
		[TestCase("15", ExpectedResult = 15, TestName = "Handles any proper value as Content-Length")]
		[TestCase("-15", ExpectedResult = -1, TestName = "Handles negative value as Content-Length")]
		[TestCase("test", ExpectedResult = 0, TestName = "Handles invalid Content-Length header")]
		public long ContentLength_get_works_properly(string contentLengthHeaderValue)
		{
			if (contentLengthHeaderValue != None)
			{
				_request.Headers["Content-Length"] = new HttpHeader("Content-Length", contentLengthHeaderValue);
			}

			return _request.ContentLength;
		}

		[TestCase(long.MinValue, TestName = "Handles negative input properly")]
		[TestCase(long.MaxValue, TestName = "Handles positive input properly")]
		[TestCase(long.MinValue, true, TestName = "Handles negative input properly when the same header is introduced previously and the new value overwrites it")]
		[TestCase(long.MaxValue, true, TestName = "Handles positive input properly when the same header is introduced previously and the negative value removes it")]
		public void ContentLength_set_works_properly(long contentLengthHeaderValue, bool previouslyContainsHeader = false)
		{
			if (previouslyContainsHeader)
			{
				// Some (not so arbitrary) familiar digits 
				_request.ContentLength = 31415926535897;
			}

			_request.ContentLength = contentLengthHeaderValue;

			if (contentLengthHeaderValue >= 0)
			{
				// Does OrdinalIgnoreCase string comparer works properly; Content-Length -> content-length
				Assert.AreEqual(_request.Headers.ContainsKey("content-length"), true);

				Assert.AreEqual(_request.Headers["Content-Length"].Value, contentLengthHeaderValue.ToString());
			}
			else
			{
				Assert.AreEqual(_request.Headers.ContainsKey("content-length"), false);
			}
		}

		[TestCase(None, ExpectedResult = false, TestName = "Works properly without Transfer-Encoding header")]
		[TestCase(null, ExpectedResult = false, TestName = "Handles null value as Transfer-Encoding")]
		[TestCase("", ExpectedResult = false, TestName = "Handles empty value as Transfer-Encoding")]
		[TestCase("chunked", ExpectedResult = true, TestName = "Works properly when Transfer-Encoding header is set to chunked")]
		[TestCase("CHUNKED", ExpectedResult = true, TestName = "Works properly when Transfer-Encoding header is set to chunked (case invariant)")]
		public bool IsChunked_get_works_properly(string transferEncodingHeaderValue)
		{
			if (transferEncodingHeaderValue != None)
			{
				_request.Headers["Transfer-Encoding"] = new HttpHeader("Transfer-Encoding", transferEncodingHeaderValue);
			}

			return _request.IsChunked;
		}


		[TestCase(true, TestName = "Works properly when IsChunked is set to true")]
		[TestCase(false, TestName = "Works properly when IsChunked is set to false")]
		[TestCase(true, true, TestName = "Works properly when IsChunked is set to true and the same header is introduced previously")]
		[TestCase(false, true, TestName = "Works properly when IsChunked is set to false and the same header is introduced previously")]
		public void IsChunked_set_works_properly(bool isChunked, bool previouslyContainsHeader = false)
		{
			if (previouslyContainsHeader)
			{
				_request.Headers["Transfer-Encoding"] = new HttpHeader("Transfer-Encoding", "test");
			}

			_request.IsChunked = isChunked;

			if (isChunked)
			{
				Assert.AreEqual(_request.Headers.ContainsKey("transfer-encoding"), true);
				Assert.AreEqual(_request.Headers["transfer-encoding"].Value, "chunked");
				Assert.AreEqual(_request.ContentLength, -1);
			}
			else
			{
				Assert.AreEqual(_request.Headers.ContainsKey("transfer-encoding"), false);
			}
		}

		[TestCase(None, ExpectedResult = false, TestName = "Works properly without Expect header")]
		[TestCase(null, ExpectedResult = false, TestName = "Handles null value as Expect")]
		[TestCase("", ExpectedResult = false, TestName = "Handles empty value as Expect")]
		[TestCase("test", ExpectedResult = false, TestName = "Handles arbitrary value as Expect")]
		[TestCase("100-continue", ExpectedResult = true, TestName = "Handles proper value as Expect")]
		[TestCase("100-Continue", ExpectedResult = true, TestName = "Handles proper value as Expect (case invariant)")]
		public bool ExpectContinue_get_works_properly(string expectHeaderValue)
		{
			if (expectHeaderValue != None)
			{
				_request.Headers["Expect"] = new HttpHeader("Expect", expectHeaderValue);
			}

			return _request.ExpectContinue;
		}
	}
}