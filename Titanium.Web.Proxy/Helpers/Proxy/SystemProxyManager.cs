using System;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Titanium.Web.Proxy.Helpers
{
	/// <summary>
	/// Manage system proxy settings
	/// </summary>
	internal class SystemProxyManager
	{
		internal const int InternetOptionSettingsChanged = 39;
		internal const int InternetOptionRefresh = 37;

		private List<HttpSystemProxyValue> _systemProxyValues;
		private bool _systemProxyEnabledPreviously;
		private bool _systemProxyConfigurationAltered;

		private bool SystemHasProxyConfiguration => _systemProxyEnabledPreviously && _systemProxyValues?.Count > 0;

		/// <summary>
		/// Sets the HTTP proxy.
		/// </summary>
		/// <param name="hostname">The hostname.</param>
		/// <param name="port">The port.</param>
		internal void SetHttpProxy(string hostname, int port)
		{
			SetProxy(hostname, port, ProxyProtocolType.Http);
		}

		/// <summary>
		/// Gets the system proxy.
		/// </summary>
		/// <param name="protocolType">Type of the protocol.</param>
		/// <returns>System proxy values from registry.</returns>
		internal HttpSystemProxyValue GetSystemProxy(ProxyProtocolType protocolType)
		{
			return SystemHasProxyConfiguration
				? _systemProxyValues?.FirstOrDefault(proxy => proxy.IsHttps == (protocolType == ProxyProtocolType.Https))
				: null;
		}

		/// <summary>
		/// Remove the http proxy setting from current machine
		/// </summary>
		internal void RemoveHttpProxy()
		{
			RemoveProxy(ProxyProtocolType.Http);
		}

		/// <summary>
		/// Set the HTTPS proxy server for current machine
		/// </summary>
		/// <param name="hostname"></param>
		/// <param name="port"></param>
		internal void SetHttpsProxy(string hostname, int port)
		{
			SetProxy(hostname, port, ProxyProtocolType.Https);
		}

		/// <summary>
		/// Removes the https proxy setting to nothing
		/// </summary>
		internal void RemoveHttpsProxy()
		{
			RemoveProxy(ProxyProtocolType.Https);
		}

		/// <summary>
		/// Sets the proxy.
		/// </summary>
		/// <param name="hostname">The hostname.</param>
		/// <param name="port">The port.</param>
		/// <param name="protocolType">Type of the protocol.</param>
		private void SetProxy(string hostname, int port, ProxyProtocolType protocolType)
		{
			var reg = Registry.CurrentUser.OpenSubKey(
				"Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

			if (reg != null)
			{
				PrepareRegistry(reg);

				var exisitingContent = reg.GetValue("ProxyServer") as string;

				if (!_systemProxyConfigurationAltered)
				{
					_systemProxyEnabledPreviously = Convert.ToInt32(reg.GetValue("ProxyEnable")) == 1;
				}

				var existingSystemProxyValues = GetSystemProxyValues(exisitingContent);

				if (!_systemProxyConfigurationAltered
					&& existingSystemProxyValues != null)
				{
					_systemProxyValues =
						existingSystemProxyValues.Select(
								proxy => new HttpSystemProxyValue { HostName = proxy.HostName, Port = proxy.Port, IsHttps = proxy.IsHttps })
							.ToList();
				}

				if (existingSystemProxyValues == null)
				{
					existingSystemProxyValues = new List<HttpSystemProxyValue>();
				}

				existingSystemProxyValues.RemoveAll(x => protocolType == ProxyProtocolType.Https ? x.IsHttps : !x.IsHttps);
				existingSystemProxyValues.Add(new HttpSystemProxyValue
				{
					HostName = hostname,
					IsHttps = protocolType == ProxyProtocolType.Https,
					Port = port
				});

				reg.SetValue("ProxyEnable", 1);
				reg.SetValue("ProxyServer", string.Join(";", existingSystemProxyValues.Select(x => x.ToString()).ToArray()));

				_systemProxyConfigurationAltered = true;
			}

			Refresh();
		}

		/// <summary>
		/// Removes the proxy.
		/// </summary>
		/// <param name="protocolType">Type of the protocol.</param>
		private void RemoveProxy(ProxyProtocolType protocolType)
		{
			var reg = Registry.CurrentUser.OpenSubKey(
				"Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
			if (reg?.GetValue("ProxyServer") != null)
			{
				var exisitingContent = reg.GetValue("ProxyServer") as string;

				var existingSystemProxyValues = GetSystemProxyValues(exisitingContent);
				existingSystemProxyValues.RemoveAll(x => protocolType == ProxyProtocolType.Https ? x.IsHttps : !x.IsHttps);

				if (existingSystemProxyValues.Count != 0)
				{
					reg.SetValue("ProxyEnable", 1);
					reg.SetValue("ProxyServer", string.Join(";", existingSystemProxyValues.Select(x => x.ToString()).ToArray()));
				}
				else
				{
					reg.SetValue("ProxyEnable", 0);
					reg.SetValue("ProxyServer", string.Empty);
				}
			}

			Refresh();
		}

		/// <summary>
		/// Removes all types of proxy settings (both http and https)
		/// </summary>
		internal void DisableAllProxy()
		{
			var reg = Registry.CurrentUser.OpenSubKey(
				"Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

			if (reg != null)
			{
				reg.SetValue("ProxyEnable", _systemProxyEnabledPreviously ? 1 : 0);

				reg.SetValue("ProxyServer",
					_systemProxyValues?.Count > 0
						? string.Join(";", _systemProxyValues.Select(x => x.ToString()).ToArray())
						: string.Empty);
			}

			_systemProxyConfigurationAltered = false;

			Refresh();
		}

		/// <summary>
		/// Get the current system proxy setting values
		/// </summary>
		/// <param name="prevServerValue"></param>
		/// <returns>Collection of system proxy configurations</returns>
		private List<HttpSystemProxyValue> GetSystemProxyValues(string prevServerValue)
		{
			var result = new List<HttpSystemProxyValue>();

			if (string.IsNullOrWhiteSpace(prevServerValue))
			{
				return result;
			}

			var proxyValues = prevServerValue.Split(';');

			if (proxyValues.Length > 0)
			{
				result.AddRange(proxyValues.Select(ParseProxyValue).Where(parsedValue => parsedValue != null));
			}
			else
			{
				var parsedValue = ParseProxyValue(prevServerValue);

				if (parsedValue != null)
				{
					result.Add(parsedValue);
				}
			}

			return result;
		}

		/// <summary>
		/// Parses the system proxy setting string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private HttpSystemProxyValue ParseProxyValue(string value)
		{
			var tmp = Regex.Replace(value, @"\s+", " ").Trim();

			if (!tmp.StartsWith("http=", StringComparison.InvariantCultureIgnoreCase)
				&& !tmp.StartsWith("https=", StringComparison.InvariantCultureIgnoreCase))
			{
				return null;
			}

			var endPoint = tmp.Split('=')[1];

			return new HttpSystemProxyValue()
			{
				HostName = endPoint.Split(':')[0],
				Port = int.Parse(endPoint.Split(':')[1]),
				IsHttps = tmp.StartsWith("https=", StringComparison.InvariantCultureIgnoreCase)
			};
		}

		/// <summary>
		/// Prepares the proxy server registry (create empty values if they don't exist) 
		/// </summary>
		/// <param name="reg"></param>
		private static void PrepareRegistry(RegistryKey reg)
		{
			if (reg.GetValue("ProxyEnable") == null)
			{
				reg.SetValue("ProxyEnable", 0);
			}

			if (reg.GetValue("ProxyServer") == null || reg.GetValue("ProxyEnable") as string == "0")
			{
				reg.SetValue("ProxyServer", string.Empty);
			}

		}

		/// <summary>
		/// Refresh the settings so that the system know about a change in proxy setting
		/// </summary>
		private void Refresh()
		{
			NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
			NativeMethods.InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
		}
	}
}