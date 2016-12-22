using NUnit.Framework;
using Titanium.Web.Proxy.Models;
using UnitTests.Titanium.Web.Proxy.Helpers;

namespace UnitTests.Titanium.Web.Proxy.Models
{
	[TestFixture]
	public class ProxyEndPointFixture
	{
		[TestCase(IpAddress.Null, ExpectedResult = false, TestName = "Should handle null input")]
		[TestCase(IpAddress.Any, ExpectedResult = false, TestName = "Should return false for IPAddress.Any")]
		[TestCase(IpAddress.Broadcast, ExpectedResult = false, TestName = "Should false for IPAddress.Broadcast")]
		[TestCase(IpAddress.IPv6Any, ExpectedResult = true, TestName = "Should return true for IPAddress.IPv6Any")]
		[TestCase(IpAddress.IPv6Loopback, ExpectedResult = true, TestName = "Should return true for IPAddress.IPv6Loopback")]
		[TestCase(IpAddress.IPv6None, ExpectedResult = true, TestName = "Should return true for IPAddress.IPv6None")]
		[TestCase(IpAddress.Loopback, ExpectedResult = false, TestName = "Should return false for IPAddress.Loopback")]
		[TestCase(IpAddress.None, ExpectedResult = false, TestName = "Should return false for IPAddress.None")]
		public bool IPV6Enabled_should_return_correct_value(IpAddress ipAddressEnum)
		{
			var ipAddress = UnitTestHelpers.GetIpAddressFromEnum(ipAddressEnum);

			return new ExplicitProxyEndPoint(ipAddress, 0, false).IpV6Enabled;
		}
	}
}