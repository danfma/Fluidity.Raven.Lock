using System;

namespace Fluidity.Raven.Lock
{
	/// <summary>
	/// A locker instance
	/// </summary>
	public interface ILocker : IDisposable
	{
		/// <summary>
		/// Extends the lifetime of the locker by the specified <paramref name="lifetime"/> from <c>now</c>.
		/// </summary>
		/// <param name="lifetime">The lifetime.</param>
		void Renew(TimeSpan lifetime);
	}
}