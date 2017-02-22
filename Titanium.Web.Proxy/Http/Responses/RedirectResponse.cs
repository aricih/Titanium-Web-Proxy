﻿namespace Titanium.Web.Proxy.Http.Responses
{
	/// <summary>
	/// Redirect response
	/// </summary>
	public sealed class RedirectResponse : Response
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RedirectResponse"/> class.
		/// </summary>
		public RedirectResponse()
		{
			ResponseStatusCode = 302;
			ResponseStatusDescription = "Found";
		}
	}
}
