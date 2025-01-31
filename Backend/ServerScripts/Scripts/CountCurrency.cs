using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CsvHelper;
using FirstLight.Game.Data;
using Quantum;
using Scripts.Base;
using ShellProgressBar;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;


[Verb("count-currency", HelpText = "count all the currency players own")]
public class CountCurrencyOptions
{
	[Option("currency", HelpText = "The game id to count", Required = true)]
	public GameId Currency { get; set; }

	[Option("segment", Default = "all")] public string SegmentName { get; set; }

	[Option('e', "environment", Default = Environment.DEV)]
	public Environment Environment { get; set; }
}


public class CountCurrency : PlayfabScript, IScriptCommandLineOptions<CountCurrencyOptions>
{
	public override Environment GetEnvironment() => Environment.PROD;

	private ulong _currencyAmount;
	private ulong _totalPlayers;

	public override void Execute(ScriptParameters parameters)
	{
		var task = RunAsync(GameId.NOOB, "all");
		task.Wait();
	}

	public async Task RunWithOptions(CountCurrencyOptions options)
	{
		SetEnvironment(options.Environment);
		if (options.SegmentName.Equals("all"))
		{
			await RunAsync(options.Currency, "all");
		}
		else
		{
			var segmentId = await GetSegmentID(options.SegmentName);
			if (segmentId == null)
			{
				throw new Exception("Cannot find segment with name " + options.SegmentName);
			}

			await RunAsync(options.Currency, segmentId);
		}
	}

	public async Task RunAsync(GameId gameId, string segmentId)
	{
		var outputFileName = await this.BatchExecutionInSegment(async (@params) => { await Process(@params, gameId); },
			segmentId: segmentId);
		var toatlFileName = outputFileName.Replace(".csv", "") + ".txt";
		File.WriteAllText(toatlFileName, "Found " + _currencyAmount + " " + gameId + " !");
	}


	private async Task Process(BatchProcessorHelper.BatchTaskParams @params, GameId id)
	{
		var playerId = @params.Profile.PlayerId;
		var state = await ReadUserState(playerId);

		if (state != null)
		{
			var playerData = state.DeserializeModel<PlayerData>();
			var value = playerData.Currencies[id];
			if (value > 0)
			{
				Interlocked.Add(ref _currencyAmount, value);
				lock (this)
				{
					@params.Info($"Found {value} {id} with player {@params.Profile.DisplayName}");
					dynamic obj = new
					{
						playerId = playerId,
						amount = value
					};
					@params.OutputWriter.WriteRecord(obj);
					@params.OutputWriter.NextRecord();
				}
			}
		}
		else
		{
			@params.Error($"Something went wrong when reading UserState for user playerId");
		}
	}
}