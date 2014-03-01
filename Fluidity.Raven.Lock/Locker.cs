using System;
using System.Diagnostics;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Abstractions.Exceptions;
using Raven.Client;

namespace Fluidity.Raven.Lock
{
	/// <summary>
	///     The locker
	/// </summary>
	public sealed class Locker : ILocker
	{
		private const int TickMilliseconds = 50;

		private readonly IDocumentStore _documentStore;
		private readonly string _lockName;
		private Lock _lock;
		private Etag _lockEtag;
		private IDocumentSession _session;

		/// <summary>
		///     Initializes a new instance of the <see cref="Locker" /> class.
		/// </summary>
		/// <param name="documentStore">The documentStore.</param>
		/// <param name="lockName">Name of the lock.</param>
		/// <param name="timeout">The timeout.</param>
		/// <param name="lifetime">The lifetime.</param>
		public Locker(IDocumentStore documentStore, string lockName, TimeSpan timeout, TimeSpan lifetime)
		{
			_documentStore = documentStore;
			_lockName = lockName;
			_session = CreateSession();

			WaitToLock(timeout, lifetime);
		}

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///     Estende o tempo de vido do lock, para garantir que a tarefa seja executada.
		/// </summary>
		/// <param name="lifetime">The lifetime.</param>
		/// <exception cref="System.NotImplementedException"></exception>
		public void Renew(TimeSpan lifetime)
		{
			_lock.Expiration = DateTime.UtcNow + lifetime;
			_session.Store(_lock, _lockEtag, _lock.Id);
			_session.SaveChanges();
			_lockEtag = _session.Advanced.GetEtagFor(_lock);
		}

		/// <summary>
		///     Finalizes an instance of the <see cref="Locker" /> class.
		/// </summary>
		~Locker()
		{
			Dispose(false);
		}

		/// <summary>
		///     Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing">
		///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
		///     unmanaged resources.
		/// </param>
		private void Dispose(bool disposing)
		{
			if (_session != null && _lock != null)
			{
				try
				{
					_session.Delete(_lock);
					_session.SaveChanges();
				}
				finally
				{
					_lock = null;
				}
			}

			if (_session != null)
				_session.Dispose();

			if (disposing)
				_session = null;
		}

		/// <summary>
		///     Creates an isolated session.
		/// </summary>
		/// <returns></returns>
		private IDocumentSession CreateSession()
		{
			IDocumentSession session = _documentStore.OpenSession();

			session.Advanced.UseOptimisticConcurrency = true;
			session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;

			return session;
		}

		/// <summary>
		///     Waits to lock to be acquired.
		/// </summary>
		/// <param name="timeout">The timeout.</param>
		/// <param name="lifetime">The lifetime.</param>
		/// <exception cref="System.TimeoutException"></exception>
		private void WaitToLock(TimeSpan timeout, TimeSpan lifetime)
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			int attempt = 0;

			do
			{
				bool expired = stopWatch.Elapsed > timeout;

				if (expired)
					throw new TimeoutException();

				if (TryAcquireLock(_lockName, lifetime, ++attempt))
					break;

				Wait();
			} while (true);
		}

		/// <summary>
		///     Try to acquire the lock.
		/// </summary>
		/// <remarks>
		///     If after to many attempts the lock is not acquired, it also checks if that lock is expired, so it could remove it.
		/// </remarks>
		/// <param name="lockName">Name of the lock.</param>
		/// <param name="lifetime">The lifetime.</param>
		/// <param name="attempt">The attempt.</param>
		/// <returns></returns>
		private bool TryAcquireLock(string lockName, TimeSpan lifetime, int attempt)
		{
			bool acquired = false;

			_lock = new Lock {
				Id = string.Format("Locks/{0}", lockName),
				Expiration = DateTime.UtcNow.Add(lifetime)
			};

			try
			{
				_session.Store(_lock, Etag.Empty, _lock.Id);
				_session.SaveChanges();
				_lockEtag = _session.Advanced.GetEtagFor(_lock);
				acquired = true;
			}
			catch (ConcurrencyException)
			{
				Trace.WriteLine("Session already locked for " + lockName);

				if (attempt%3 == 0)
					RemoveExpiredLock(_lock.Id);

				_lock = null;
				_session.Advanced.Clear();
			}

			return acquired;
		}

		/// <summary>
		///     Removes the expired lock.
		/// </summary>
		/// <param name="lockId">The lock identifier.</param>
		private void RemoveExpiredLock(string lockId)
		{
			using (IDocumentSession session = _documentStore.OpenSession())
			{
				var lockInstance = session.Load<Lock>(lockId);

				if (lockInstance != null && lockInstance.Expired)
				{
					session.Delete(lockInstance);
					session.SaveChanges();
				}
			}
		}

		/// <summary>
		///     Waits for some time.
		/// </summary>
		private void Wait()
		{
			Thread.Sleep(TickMilliseconds);
		}
	}
}