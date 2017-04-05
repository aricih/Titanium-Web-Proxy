using NUnit.Framework;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.UnitTests.Helpers
{
	[TestFixture]
	public class HttpRequestHeadParserFixture
	{
		[TestCase(null, ExpectedResult = null, TestName = "Handles null input properly")]
		[TestCase("", ExpectedResult = null, TestName = "Handles empty input properly")]
		[TestCase("Some arbitrary input", ExpectedResult = null, TestName = "Handles any arbitrary input properly")]
		[TestCase("GET a.com HTTP/1.0", ExpectedResult = "GET,a.com,1.0", TestName = "Parses proper input correctly")]
		[TestCase("GET    a.com   HTTP/1.0", ExpectedResult = "GET,a.com,1.0", TestName = "Parses input with extra spaces correctly")]
		public string Parse_works_properly(string httpCommand)
		{
			var requestHead = HttpRequestHeadParser.Parse(httpCommand);

			return requestHead != null ? string.Join(",", requestHead.Method, requestHead.Url, requestHead.Version) : null;
		}
	}
}