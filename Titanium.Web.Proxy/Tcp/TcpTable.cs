using System.Collections;
using System.Collections.Generic;

namespace Titanium.Web.Proxy.Tcp
{
	/// <summary>
	/// Represents collection of TcpRows
	/// </summary>
	/// <seealso>
	///     <cref>System.Collections.Generic.IEnumerable{Titanium.Web.Proxy.Tcp.TcpRow}</cref>
	/// </seealso>
	internal class TcpTable : IEnumerable<TcpRow>
	{
		private readonly IEnumerable<TcpRow> _tcpRows;

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpTable"/> class.
		/// </summary>
		/// <param name="tcpRows">TcpRow collection to initialize with.</param>
		public TcpTable(IEnumerable<TcpRow> tcpRows)
		{
			_tcpRows = tcpRows;
		}

		/// <summary>
		/// Gets the TCP rows.
		/// </summary>
		public IEnumerable<TcpRow> TcpRows => _tcpRows;

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<TcpRow> GetEnumerator()
		{
			return _tcpRows.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}