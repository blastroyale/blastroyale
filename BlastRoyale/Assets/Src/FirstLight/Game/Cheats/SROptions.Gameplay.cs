using System;
using System.Collections.Generic;
using System.ComponentModel;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public partial class SROptions
{
	[Category("Gameplay")]
	public void CrashUnity()
	{
		Utils.ForceCrash(ForcedCrashCategory.FatalError);
	}
#if !DISABLE_SRDEBUGGER

	private static void AddQuantumCheats()
	{
		var category = "Gameplay";
		var container = new SRDebugger.DynamicOptionContainer();
		// Create a mutable option

		var values = new Dictionary<string, Func<DeterministicCommand>>()
		{
			{"Kill Local Player", () => new CheatLocalPlayerKillCommand()},
			{"Win Local Player", () => new CheatCompleteKillCountCommand {IsLocalWinner = true}},
			{"Win Other Player", () => new CheatCompleteKillCountCommand()},
			{"Refill Ammo And Specials", () => new CheatRefillAmmoAndSpecials()},
			{"Kill all except 1", () => new CheatKillAllExceptCommand() {Amount = 1}},
			{"Kill all except 2", () => new CheatKillAllExceptCommand() {Amount = 2}},
			{"Kill team mates", () => new CheatKillTeamMatesCommand()},
			{"Spawn air drop here", () => new CheatSpawnAirDropCommand {OnPlayerPosition = true}},
			{"Spawn air drop random", () => new CheatSpawnAirDropCommand {OnPlayerPosition = false}},
			{"Spawn all specials", () => new CheatSpawnAllSpecialsCommand()},
			{"Spawn all weapons", () => new CheatSpawnAllWeaponsCommand()},
			{"Spawn all golden weapons", () => new CheatSpawnAllWeaponsCommand() {Golden = true}},
		};
		foreach (var kv in values)
		{
			container.AddOption(SRDebugger.OptionDefinition.FromMethod(kv.Key, () =>
			{
				var game = QuantumRunner.Default.Game;
				if (game == null)
				{
					Debug.LogWarning("Simulation is not running yet");
					return;
				}

				game.SendCommand(kv.Value());
			}, category));
		}

		SRDebug.Instance.AddOptionContainer(container);
	}
#endif
}