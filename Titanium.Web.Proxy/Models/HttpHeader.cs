using System;

namespace Titanium.Web.Proxy.Models
{
	/// <summary>
	/// Http Header object used by proxy
	/// </summary>
	public class HttpHeader
	{
		private string _name;
		private string _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpHeader"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="Exception">Name cannot be null</exception>
		public HttpHeader(string name, string value)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new Exception("Name cannot be null");
			}

			Name = name;
			Value = value;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name
		{
			get { return _name; }
			set { _name = value?.Trim(); }
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public string Value
		{
			get { return _value; }
			set { _value = value?.Trim(); }
		}

		/// <summary>
		/// Returns header as a valid header string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{Name}: {Value}";
		}
	}
}