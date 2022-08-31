
using System;
using Backend.Models;
using NUnit.Framework;
using ServerSDK.Models;


[Serializable]
class TestData
{
	public string Property;
}


public class TestDataMigration
{
	private StateMigrations _migrations;
	
	[SetUp]
	public void Setup()
	{
		_migrations = new StateMigrations();
	}
	
	[Test]
	public void TestUnversionedWorksFine()
	{
		var state = new ServerState();
		
		Assert.IsFalse(state.ContainsKey("version"));

		_migrations.RunMigrations(state);
		
		Assert.IsTrue(state.ContainsKey("version"));
		Assert.AreEqual(_migrations.CurrentVersion, state.GetVersion());

	}
	
	[Test]
	public void TestBaseMigration()
	{
		var model = new TestData() {Property = "Old Value"};
		var updatedModel = new TestData() {Property = "New Value"};
		
		var serverState = new ServerState();
		serverState.UpdateModel(model);
		serverState.SetVersion(0);
		
		// Adding V0-> V1 migration
		_migrations.CurrentVersion = 1;
		_migrations.Migrations[0] = state => state.UpdateModel(updatedModel);

		var versionBumps = _migrations.RunMigrations(serverState);

		Assert.AreEqual(1, versionBumps);
		Assert.AreEqual(updatedModel.Property, serverState.DeserializeModel<TestData>().Property);
	}
	
}