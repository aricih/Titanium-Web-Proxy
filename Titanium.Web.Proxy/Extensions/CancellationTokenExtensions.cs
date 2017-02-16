using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Extensions
{
	/// <summary>
	/// Implements CancellationToken extension methods.
	/// </summary>
	public static class CancellationTokenExtensions
	{
		/// <summary>
		/// Returns an awaitable Task from CancellationToken.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task WhenCancelled(this CancellationToken cancellationToken)
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();

			cancellationToken.Register(source => ((TaskCompletionSource<bool>) source).SetResult(true), taskCompletionSource);

			return taskCompletionSource.Task;
		}
	}
}