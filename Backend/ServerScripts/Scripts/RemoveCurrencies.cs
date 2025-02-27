using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CsvHelper;
using FirstLight.Game.Data;
using Microsoft.AspNetCore.Components.Forms;
using PlayFab.ServerModels;
using Quantum;
using Scripts.Base;

namespace Scripts.Scripts
{
	[Verb("remove-currency", HelpText = "Remove currency from players based on a csv file")]
	public class RemoveCurrencyFromCSVOptions
	{
		[Option("currency", HelpText = "The game id to count", Required = true)]
		public GameId Currency { get; set; }

		[Option("input-file", Required = true)]
		public string InputFile { get; set; }

		[Option("column", Required = true, HelpText = "index(starting with 0) or name")]
		public string Column { get; set; }


		[Option("header", Required = false, HelpText = "input file has header", Default = false)]
		public bool HasHeader { get; set; }


		[Option('e', "environment", Default = Environment.DEV)]
		public Environment Environment { get; set; }
	}

	/// <summary>
	/// Script to replace Hammers in all players inventories to a random weapon
	/// Hammer in player inventory should be an invalid state of the data.  
	/// </summary>
	public class RemoveCurrencyFromCSV : PlayfabScript, IScriptCommandLineOptions<RemoveCurrencyFromCSVOptions>
	{
		private HubService _hubService;

		public async Task RunWithOptions(RemoveCurrencyFromCSVOptions options)
		{
			SetEnvironment(options.Environment);
			_hubService = new HubService();
			if (File.Exists(options.InputFile) && !options.InputFile.EndsWith(".csv"))
			{
				throw new Exception($"File not {options.InputFile} found or not a csv!");
			}

			await this.BatchExecutionInCsv((arg) => ProcessPlayer(arg, options.Currency), options.InputFile,
				options.Column, options.HasHeader,
				maxTasks: 20);
		}

		private SemaphoreSlim writterSemaphore = new SemaphoreSlim(1);

		private async Task ProcessPlayer(BatchProcessorHelper.CsvBatchTaskParameters arg, GameId currency)
		{
			var ammount = arg.RawRow[1];
			var wallet = arg.RawRow[2];
			var hasWallet = !string.IsNullOrWhiteSpace(wallet?.ToString());
			if (!hasWallet)
			{
				return;
			}

			var state = await ReadUserState(arg.PlayerId);

			var playerData = state.DeserializeModel<PlayerData>();
			var ownedAmount = playerData.Currencies[currency];
			if (ownedAmount!.ToString() != ammount.ToString())
			{
				arg.Error($"Missmatch amount for {arg.PlayerId} owned:{ownedAmount} amount:{ammount}");
			}

			playerData.Currencies[currency] = 0;
			state.UpdateModel(playerData);
			await SetUserState(arg.PlayerId, state);
		}

		public override Environment GetEnvironment()
		{
			return Environment.PROD;
		}

		public override void Execute(ScriptParameters args)
		{
			throw new Exception("This script doesn't support interactive mode :(");
		}

		private async Task WipeTutorialSections(PlayerProfile profile)
		{
			var state = await ReadUserState(profile.PlayerId);
			if (state == null)
			{
				return;
			}

			state.UpdateModel(new TutorialData());

			await SetUserState(profile.PlayerId, state);
		}
	}
}