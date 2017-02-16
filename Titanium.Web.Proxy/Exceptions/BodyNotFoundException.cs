namespace Titanium.Web.Proxy.Exceptions
{
	/// <summary>
	/// An expception thrown when body is unexpectedly empty
	/// </summary>
	public class BodyNotFoundException : ProxyException
	{
		/// <summary>
		/// Instantiate a new instance of this exception - must be invoked by derived classes' constructors
		/// </summary>
		/// <param name="message">Exception message</param>
		public BodyNotFoundException(string message)
			: base(message)
		{
		}
	}
}