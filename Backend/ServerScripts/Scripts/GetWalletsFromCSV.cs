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
	[Verb("wallet-csv", HelpText = "read all playerids and generate an .csv file with their wallets")]
	public class Options
	{
		[Option("input-file", Required = true)]
		public string InputFile { get; set; }

		[Option("column", Required = true, HelpText = "index(starting with 0) or name")]
		public string Column { get; set; }


		[Option("header", Required = false, HelpText = "input file has header", Default = false)]
		public bool HasHeader { get; set; }
	}

	/// <summary>
	/// Script to replace Hammers in all players inventories to a random weapon
	/// Hammer in player inventory should be an invalid state of the data.  
	/// </summary>
	public class GetWalletsFromCSV : PlayfabScript, IScriptCommandLineOptions<Options>
	{
		private HubService _hubService;

		public async Task RunWithOptions(Options options)
		{
			_hubService = new HubService();
			if (File.Exists(options.InputFile) && !options.InputFile.EndsWith(".csv"))
			{
				throw new Exception($"File not {options.InputFile} found or not a csv!");
			}

			await this.BatchExecutionInCsv(ProcessPlayer, options.InputFile, options.Column, options.HasHeader,
				maxTasks: 20);
		}

		private SemaphoreSlim writterSemaphore = new SemaphoreSlim(1);

		private async Task ProcessPlayer(BatchProcessorHelper.CsvBatchTaskParameters arg)
		{
			string wallet = "null";
			try
			{
				wallet = await _hubService.FetchWalletAddressFromPlayerIdAsync(arg.PlayerId);
			}
			catch (Exception ex)
			{
				wallet = "failed";
				arg.Error(ex.Message);
				Console.WriteLine("PROGRAM FAILED !!!" + ex.Message);
				System.Environment.Exit(1);
			}

			arg.Info("Found wallet " + wallet + " for player " + arg.PlayerId);
			try
			{
				await writterSemaphore.WaitAsync();
				foreach (var o in arg.RawRow)
				{
					arg.OutputWriter.WriteField(o);
				}


				arg.OutputWriter.WriteField(wallet);
				await arg.OutputWriter.NextRecordAsync();
			}
			finally
			{
				writterSemaphore.Release();
			}
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