using System;
using System.Collections;
using NUnit.Framework;
using Titanium.Web.Proxy.Extensions;

namespace Titanium.Web.Proxy.UnitTests.Extensions
{
	public class ByteArrayExtensionsFixture
	{
		[TestCaseSource(typeof(ByteArrayExtensionsTestCaseSource), nameof(ByteArrayExtensionsTestCaseSource.SubArrayTestCases))]
		public int[] SubArray_works_properly(int[] array, int index, int length)
		{
			return array.SubArray(index, length);
		}

		private class ByteArrayExtensionsTestCaseSource
		{
			public static IEnumerable SubArrayTestCases
			{
				get
				{
					yield return new TestCaseData(Array.Empty<int>(), 0, 0)
						.Returns(null)
						.SetName("Handles empty array");

					yield return new TestCaseData(Array.Empty<int>(), -1, 0)
						.Returns(null)
						.SetName("Handles invalid index");

					yield return new TestCaseData(Array.Empty<int>(), 10, 1)
						.Returns(null)
						.SetName("Handles out of range index caused by index parameter");

					yield return new TestCaseData(Array.Empty<int>(), 0, 10)
						.Returns(null)
						.SetName("Handles out of range index caused by length parameter with empty array");

					yield return new TestCaseData(new [] { 0, 1, 2, 4, 8, 16, 32 }, 3, 10)
						.Returns(new [] { 4, 8, 16, 32 })
						.SetName("Handles out of range index caused by length parameter with non-empty array");
				}
			}
		}
	}
}