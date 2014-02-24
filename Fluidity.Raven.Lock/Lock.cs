using System;
using Raven.Imports.Newtonsoft.Json;

namespace Fluidity.Raven.Lock
{
	public sealed class Lock
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="Lock" /> class.
		/// </summary>
		[JsonConstructor]
		public Lock()
		{
		}

		/// <summary>
		///     Identifica��o do objeto
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		///     A data de expira��o para o lock.
		/// </summary>
		public DateTime Expiration { get; set; }

		/// <summary>
		///     Determina se este lock j� foi expirado.
		/// </summary>
		[JsonIgnore]
		public bool Expired
		{
			get { return DateTime.UtcNow > Expiration; }
		}
	}
}