
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServerSDK.Services;

namespace Tests;

public class TestServerMutex
{
	private TestServer _server = null!;
	
	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
	}

	[Test]
	public void TestMutexBlock()
	{
		var mutex = _server.GetService<IServerMutex>();
		var task = Task.Run(async () =>
		{	
			await mutex.Lock("AnyUser");
			await mutex.Lock("AnyUser");
		});
		var completedInTime = task.Wait(TimeSpan.FromSeconds(2));
		Assert.IsFalse(completedInTime);
	}
	
	[Test]
	public void TestUnlocking()
	{
		var mutex = _server.GetService<IServerMutex>();
		var task = Task.Run(async () =>
		{	
			await mutex.Lock("SomeUser");
			mutex.Unlock("SomeUser");
			await mutex.Lock("SomeUser");
		});
		var completedInTime = task.Wait(TimeSpan.FromSeconds(3));
		Assert.IsTrue(completedInTime);
	}
	
	[Test]
	public void TestMutexNotBlockDifferentKeys()
	{
		var mutex = _server.GetService<IServerMutex>();
		var task = Task.Run(async () =>
		{	
			await mutex.Lock("AnyUser_1");
			await mutex.Lock("AnyUser_2");
		});
		var completedInTime = task.Wait(TimeSpan.FromSeconds(2));
		Assert.IsTrue(completedInTime);
	}
}