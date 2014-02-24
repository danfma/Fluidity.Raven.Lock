using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Raven.Client;

namespace Fluidity.Raven.Lock.Tests
{
	[TestFixture]
	public class LockTests
	{
		[SetUp]
		public void BeforeEach()
		{
			_documentSession = _documentStore.OpenSession();
		}

		[TearDown]
		public void AfterEach()
		{
			_documentSession.Dispose();
		}

		private IDocumentStore _documentStore;
		private IDocumentSession _documentSession;

		[TestFixtureSetUp]
		public void BeforeAll()
		{
			//_documentStore = new DocumentStoreFactory().Create("InMemory");
			// TODO Download the Raven.Embedded and use it on memory (there is a problem with the current version on nuget).
		}

		[TestFixtureTearDown]
		public void AfterAll()
		{
			_documentStore.Dispose();
		}

		public class SharedObject
		{
			public string Id { get; set; }
			public int Coins { get; set; }
		}

		[Test]
		public void LockExpiration()
		{
			using (IDocumentSession session = _documentStore.OpenSession())
			{
				Assert.That(session.Load<Lock>("Locks/A"), Is.Null);
				session.Advanced.Clear();

				var lockInstance = new Lock
				{
					Id = "Locks/A",
					Expiration = DateTime.UtcNow.AddMilliseconds(500)
				};

				session.Store(lockInstance, lockInstance.Id);
				session.SaveChanges();
				session.Advanced.Clear();

				Task task = Task.Factory.StartNew(() =>
				{
					using (session.Lock("A"))
					{
						Console.WriteLine("Locked");
					}
				});

				task.Wait(TimeSpan.FromMinutes(20));
			}
		}

		[Test]
		public void LockExpirationAndRetain()
		{
			using (IDocumentSession session = _documentStore.OpenSession())
			{
				Assert.That(session.Load<Lock>("Locks/A"), Is.Null);
				session.Advanced.Clear();
			}

			bool locked = false;


			Task task1 = Task.Factory.StartNew(() =>
			{
				Console.WriteLine("Starting Task 1");

				using (IDocumentSession session = _documentStore.OpenSession())
				using (ILocker locker = session.Lock("A", TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(5000)))
				{
					locked = true;

					for (int i = 0; i < 5; i++)
					{
						Console.WriteLine("(Task 1) Renew lock");
						locker.Renew(TimeSpan.FromMilliseconds(5000));
						Thread.Sleep(100);
					}

					Console.WriteLine("(Task 1) Task done");
				}
			});

			Task task2 = Task.Factory.StartNew(() =>
			{
				while (!locked)
				{
					Console.Write(".");
					Thread.Sleep(100);
				}

				Console.WriteLine("Starting Task 2");

				using (IDocumentSession session = _documentStore.OpenSession())
				using (session.Lock("A"))
				{
					Console.WriteLine("(Task 2) Finally Locked");
				}
			});

			Task.WaitAll(task1, task2);
		}

		[Test]
		public void Lock_Alone()
		{
			int counter = 0;

			using (_documentSession.Lock("MyLock/1"))
			{
				counter++;
			}

			Assert.That(counter, Is.EqualTo(1));
		}

		[Test]
		public void Lock_SixTasks()
		{
			var tasks = new[]
			{
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/2"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/3"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/2"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				})
			};

			int counter = Task.WhenAll(tasks).Result.Sum();

			Assert.That(counter, Is.EqualTo(6));
		}

		[Test]
		public void Lock_ThreeTasks()
		{
			var tasks = new[]
			{
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/2"))
					{
						Thread.Sleep(200);
						return 1;
					}
				})
			};

			int counter = Task.WhenAll(tasks).Result.Sum();

			Assert.That(counter, Is.EqualTo(3));
		}

		[Test]
		public void Lock_TwoTasks_SameSession_DifferentLocks()
		{
			var tasks = new[]
			{
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(100);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/2"))
					{
						Thread.Sleep(100);
						return 1;
					}
				})
			};

			int counter = Task.WhenAll(tasks).Result.Sum();

			Assert.That(counter, Is.EqualTo(2));
		}

		[Test]
		public void Lock_TwoTasks_SameSession_SameLocks()
		{
			var tasks = new[]
			{
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				}),
				Task<int>.Factory.StartNew(() =>
				{
					using (_documentSession.Lock("MyLock/1"))
					{
						Thread.Sleep(200);
						return 1;
					}
				})
			};

			int counter = Task.WhenAll(tasks).Result.Sum();

			Assert.That(counter, Is.EqualTo(2));
		}

		[Test]
		public void MultipleLocks()
		{
			using (IDocumentSession session = _documentStore.OpenSession())
			{
				session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;

				var sharedObject = new SharedObject
				{
					Coins = 0
				};

				session.Store(sharedObject);
				session.SaveChanges();

				string sharedObjectId = session.Advanced.GetDocumentId(sharedObject);
				Lock lockInstance;

				Assert.That(session.Load<Lock>("Locks/A"), Is.Null);
				session.Advanced.Clear();

				Action<int> action = coins =>
				{
					using (IDisposable locker = session.Lock("A"))
					{
						lockInstance = session.Load<Lock>("Locks/A");
						Assert.That(locker, Is.Not.Null);

						sharedObject = session.Load<SharedObject>(sharedObjectId);
						sharedObject.Coins += coins;

						session.Store(sharedObject);
						session.SaveChanges();

						Thread.Sleep(100);

						session.Advanced.Clear();
					}
				};

				Task[] tasks = Enumerable.Range(0, 32)
					.Select(x => x % 2 == 0 ? 2 : -1)
					.Select(x => Task.Factory.StartNew(() => action(x)))
					.ToArray();

				Task.WaitAll(tasks);

				session.Advanced.Clear();
				sharedObject = session.Load<SharedObject>(sharedObjectId);

				Assert.That(sharedObject.Coins, Is.EqualTo(tasks.Count() / 2));
			}
		}
	}
}