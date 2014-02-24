using System;
using Raven.Client;

namespace Fluidity.Raven.Lock
{
	public static class DocumentSessionExtensions
	{
		/// <summary>
		///     Creates a lock with the specified name.
		/// </summary>
		/// <param name="documentSession">The document session.</param>
		/// <param name="lockName">Name of the lock.</param>
		/// <returns></returns>
		public static ILocker Lock(this IDocumentSession documentSession, string lockName)
		{
			return Lock(documentSession, lockName, TimeSpan.FromSeconds(30));
		}

		/// <summary>
		///     Creates a lock with the specified name.
		/// </summary>
		/// <param name="documentSession">The document session.</param>
		/// <param name="lockName">Name of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		/// <returns></returns>
		public static ILocker Lock(this IDocumentSession documentSession, string lockName, TimeSpan timeout)
		{
			return Lock(documentSession, lockName, timeout, TimeSpan.FromMinutes(1));
		}

		/// <summary>
		///     Creates a lock with the specified name.
		/// </summary>
		/// <param name="documentSession">The document session.</param>
		/// <param name="lockName">Name of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		/// <param name="lifetime">The lifetime.</param>
		/// <returns></returns>
		public static ILocker Lock(this IDocumentSession documentSession, string lockName, TimeSpan timeout, TimeSpan lifetime)
		{
			IDocumentStore store = documentSession.Advanced.DocumentStore;

			return new Locker(store, lockName, timeout, lifetime);
		}
	}
}