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
			StatusCode = 200;
			StatusDescription = "Ok";
		}
	}
}
