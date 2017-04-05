using NUnit.Framework;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.UnitTests.Helpers
{
	[TestFixture]
	public class HttpVersionParserFixture
	{
		[TestCase(null, HttpCommandType.Request, ExpectedResult = null, TestName = "Handles null input properly")]
		[TestCase("", HttpCommandType.Response, ExpectedResult = null, TestName = "Handles empty input properly")]
		[TestCase("test", HttpCommandType.Request, ExpectedResult = null, TestName = "Handles irrelevant input properly")]
		[TestCase("HTTP/", HttpCommandType.Response, ExpectedResult = null, TestName = "Handles incomplete input properly")]
		[TestCase("HTTP/asd", HttpCommandType.Request, ExpectedResult = null, TestName = "Handles invalid version properly")]
		[TestCase("HTTP/1.0", HttpCommandType.Response, ExpectedResult = "1.0", TestName = "Parses proper input correcly")]
		public string Parse_works_properly(string httpVersion, HttpCommandType commandType)
		{
			var version = HttpVersionParser.Parse(
				commandType == HttpCommandType.Request
					? new []{ null, null, httpVersion }
					: new []{ httpVersion, null, null},
				commandType);
			
			return version?.ToString();
		}
	}
}