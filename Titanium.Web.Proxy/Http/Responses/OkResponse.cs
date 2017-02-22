namespace Titanium.Web.Proxy.Http.Responses
{
	/// <summary>
	/// 200 Ok response
	/// </summary>
	public sealed class OkResponse : Response
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OkResponse"/> class.
		/// </summary>
		public OkResponse()
		{
			ResponseStatusCode = 200;
			ResponseStatusDescription = "Ok";
		}
	}
}
