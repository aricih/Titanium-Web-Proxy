using NUnit.Framework;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.UnitTests.Helpers
{
	[TestFixture]
	public class HttpSystemProxyValueFixture
	{
		[TestCase(null, 0, false, ExpectedResult = "", TestName = "Handles null hostname")]
		[TestCase("", 0, false, ExpectedResult = "", TestName = "Handles empty hostname")]
		[TestCase("127.0.0.1", 8080, true, ExpectedResult = "https=127.0.0.1:8080", TestName = "Handles proper proxy value correctly")]
		public string ToString_works_properly(string hostName, int port, bool isHttps)
		{
			var systemProxy = new HttpSystemProxyValue { HostName = hostName, Port = port, IsHttps = isHttps };

			return systemProxy.ToString();
		}
	}
}