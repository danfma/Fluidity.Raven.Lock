using System;

namespace Fluidity.Raven.Lock
{
	public interface IEtagAware
	{
		Guid Etag { get; set; }
	}
}