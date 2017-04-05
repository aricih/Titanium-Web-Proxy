using System.Net;
using NSubstitute;

namespace Titanium.Web.Proxy.UnitTests.Helpers
{
	internal static class UnitTestHelpers
	{
		/// <summary>
		/// Gets the fake object.
		/// </summary>
		/// <typeparam name="T">The type.</typeparam>
		/// <returns>Fake object.</returns>
		public static T GetFakeObject<T>(params object[] constructorArguments) where T : class
		{
			return Substitute.For<T>(constructorArguments);
		}

		/// <summary>
		/// Gets the ip address from enum.
		/// </summary>
		/// <param name="ipAddress">The IpAddress enum value.</param>
		/// <returns>IPAddress instance.</returns>
		public static IPAddress GetIpAddressFromEnum(IpAddress ipAddress)
		{
			IPAddress result = null;

			switch (ipAddress)
			{
				case IpAddress.Null:
					break;
				case IpAddress.Any:
					result = IPAddress.Any;
					break;
				case IpAddress.Broadcast:
					result = IPAddress.Broadcast;
					break;
				case IpAddress.IPv6Any:
					result = IPAddress.IPv6Any;
					break;
				case IpAddress.IPv6Loopback:
					result = IPAddress.IPv6Loopback;
					break;
				case IpAddress.IPv6None:
					result = IPAddress.IPv6None;
					break;
				case IpAddress.Loopback:
					result = IPAddress.Loopback;
					break;
				case IpAddress.None:
					result = IPAddress.None;
					break;
			}

			return result;
		}
	}
}