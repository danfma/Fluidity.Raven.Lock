Fluidity.Raven.Lock
===================

A simple way, without server change, to allow pessimistic locks in RavenDB, using the current optimistic lock feature.

It's not a perfect way but if the Ayende put this in Raven, then he could optimize it in a better way.

## Install by Nuget

```Install-Package Fluidity.Raven.Lock```

## Usage

```C#
using (var locker = documentSession.Lock("MyLockName"))
{
	// do some work
}
```

You can configure a timeout to grab the lock, so if it fails to lock in the time it will raise a ```System.TimeoutException```.

Because the lock is **stored on the server** and **managed by the client**, it was necessary to create some security way to not lock forever with a key, so each lock has an expiration time defined.

If another lock tries to grant his way by using a key and it fails for some tries, it will check if the lock is an expired lock, then delete it and try again to grab the lock.

You can set the timeout or the lifetime of the lock when creating it, by using some of the methods signatures ahead:

* ```ILocker Lock(this IDocumentSession documentSession, string lockName)```;
* ```ILocker Lock(this IDocumentSession documentSession, string lockName, TimeSpan timeout)```;
* ```ILocker Lock(this IDocumentSession documentSession, string lockName, TimeSpan timeout, TimeSpan lifetime)```;

**Important**

* The default timeout for waiting to lock is 30 seconds;
* The default lifetime of a lock is 1 minute.
* If for some reason, you need to create a lock and execute some undeterministic algorithm, and you don't want to increase the lifetime of the lock so much, you can use this:

```C#
using (var locker = documentSession.Lock("MyLockName"))
{
	// do some work
	locker.Renew(TimeSpan.FromMinutes(1)); // expanding the lifetime in more 1 minute from now
	// more work
	locker.Renew(TimeSpan.FromMinutes(1)); // expanding the lifetime in more 1 minute from now
}
```
