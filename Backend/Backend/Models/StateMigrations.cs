using System;
using System.Collections.Generic;
using ServerSDK.Models;

namespace Backend.Models;

/// <summary>
/// Interface to implement eventual state migrators to allow us to serialize data in different formats
/// or perform logical updates on our data.
/// </summary>
public interface IStateMigrator<TStateType>
{
	/// <summary>
	/// Runs all necessary migrations given a specific state.
	/// Should return the number of version bumps on the given state.
	/// </summary>
	int RunMigrations(TStateType state);
}

/// <summary>
/// Object responsible for dealing with serialized model migrations.
/// </summary>
public class StateMigrations: IStateMigrator<ServerState>
{
	public ulong CurrentVersion = 1;

	public Dictionary<ulong, Action<ServerState>> Migrations = new ()
	{
		// { 1, (ServerState state) => state["somefield"] = "somevalue"  }
	};

	public int RunMigrations(ServerState state)
	{
		var versionBumps = 0;
		var versionNumber = state.GetVersion();
		if (versionNumber < CurrentVersion)
		{
			while (versionNumber < CurrentVersion)
			{
				if (Migrations.TryGetValue(versionNumber, out var migrationAction))
				{
					migrationAction(state);
				}
				versionNumber++;
				versionBumps++;
			}
		}
		state.SetVersion(versionNumber);
		return versionBumps;
	}
}