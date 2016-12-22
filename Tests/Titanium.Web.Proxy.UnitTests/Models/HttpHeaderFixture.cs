using System;
using NUnit.Framework;
using Titanium.Web.Proxy.Models;

namespace UnitTests.Titanium.Web.Proxy.Models
{
	[TestFixture]
	public class HttpHeaderFixture
	{
		[TestCase("name", null, ExpectedResult = "name:", TestName = "Should handle null value")]
		[TestCase("name", "", ExpectedResult = "name:", TestName = "Should handle empty value")]
		[TestCase("name", "value", ExpectedResult = "name: value", TestName = "Should serialize proper header correctly")]
		public string ToString_should_serialize_header_correctly(string name, string value)
		{
			return new HttpHeader(name, value).ToString().Trim();
		}

		[Test]
		public void Null_or_empty_header_name_should_throw_exception()
		{
			Assert.Throws<Exception>(() => new HttpHeader(null, string.Empty));
			Assert.Throws<Exception>(() => new HttpHeader(string.Empty, string.Empty));
		}
	}
}