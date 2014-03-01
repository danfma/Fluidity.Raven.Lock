using System;
using Raven.Imports.Newtonsoft.Json;

namespace Fluidity.Raven.Lock
{
	/// <summary>
	///     The lock instance that will be created on the database.
	/// </summary>
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
		///     Gets or sets the identifier.
		/// </summary>
		/// <value>
		///     The identifier.
		/// </value>
		public string Id { get; set; }

		/// <summary>
		///     Gets or sets the expiration date.
		/// </summary>
		/// <value>
		///     The expiration date.
		/// </value>
		public DateTime Expiration { get; set; }

		/// <summary>
		///     Gets a value indicating whether this lock has expired.
		/// </summary>
		/// <remarks>
		///     This property is not persitent.
		/// </remarks>
		/// <value>
		///     <c>true</c> if has expired; otherwise, <c>false</c>.
		/// </value>
		[JsonIgnore]
		public bool Expired
		{
			get { return DateTime.UtcNow > Expiration; }
		}
	}
}