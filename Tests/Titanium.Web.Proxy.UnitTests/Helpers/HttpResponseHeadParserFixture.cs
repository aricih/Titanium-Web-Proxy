using NUnit.Framework;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.UnitTests.Helpers
{
	[TestFixture]
	public class HttpResponseHeadParserFixture
	{
		[TestCase(null, ExpectedResult = null, TestName = "Handles null input properly")]
		[TestCase("", ExpectedResult = null, TestName = "Handles empty input properly")]
		[TestCase("Some arbitrary input", ExpectedResult = null, TestName = "Handles any arbitrary input properly")]
		[TestCase("HTTP/1.0 200 OK", ExpectedResult = "1.0,200,OK", TestName = "Parses proper input correctly")]
		[TestCase("HTTP/1.0   200   OK", ExpectedResult = "1.0,200,OK", TestName = "Parses input with extra spaces correctly")]
		public string Parse_works_properly(string httpCommand)
		{
			var responseHead = HttpResponseHeadParser.Parse(httpCommand);

			return responseHead != null ? string.Join(",", responseHead.Version, responseHead.StatusCode, responseHead.StatusDescription) : null;
		}
	}
}