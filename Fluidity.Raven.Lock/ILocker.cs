using System;

namespace Fluidity.Raven.Lock
{
	public interface ILocker : IDisposable
	{
		/// <summary>
		/// Estende o tempo de vido do lock, para garantir que a tarefa seja executada.
		/// </summary>
		/// <param name="lifetime">The lifetime.</param>
		void Renew(TimeSpan lifetime);
	}
}