namespace Titanium.Web.Proxy.Helpers
{
	internal class HttpSystemProxyValue
	{
		internal string HostName { get; set; }
		internal int Port { get; set; }
		internal bool IsHttps { get; set; }

		public override string ToString()
		{
			return string.IsNullOrEmpty(HostName) 
				? string.Empty 
				: $"{(IsHttps ? "https" : "http")}={HostName}:{Port}";
		}
	}
}