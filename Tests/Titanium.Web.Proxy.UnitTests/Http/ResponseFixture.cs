using NUnit.Framework;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace UnitTests.Titanium.Web.Proxy.Http
{
	[TestFixture]
	public class ResponseFixture
	{
		private const string None = "None";

		[TestCase(None, ExpectedResult = null, TestName = "Works properly without Content-Type header")]
		[TestCase(null, ExpectedResult = null, TestName = "Handles null value as Content-Type")]
		[TestCase("", ExpectedResult = "", TestName = "Handles empty value as Content-Type")]
		[TestCase("text/html; charset=utf-8", ExpectedResult = "text/html; charset=utf-8", TestName = "Handles any proper value")]
		public string ContentType_get_works_properly(string contentTypeHeaderValue)
		{
			var response = new Response();

			if (contentTypeHeaderValue != None)
			{
				response.ResponseHeaders["Content-Type"] = new HttpHeader("Content-Type", contentTypeHeaderValue);
			}

			return response.ContentType;
		}

	}
}