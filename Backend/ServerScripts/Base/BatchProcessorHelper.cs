using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ShellProgressBar;
using PlayerProfile = PlayFab.ServerModels.PlayerProfile;

namespace Scripts.Base;

public static class BatchProcessorHelper
{
	public class BatchTaskParams
	{
		public PlayerProfile Profile;
		public Action<string> Info { get; private set; }
		public Action<string> Error { get; private set; }
		public CsvWriter OutputWriter { get; private set; }

		public BatchTaskParams(PlayerProfile profile, Action<string> info, Action<string> error, CsvWriter outputWriter)
		{
			Profile = profile;
			Info = info;
			Error = error;
			OutputWriter = outputWriter;
		}
	}

	public class CsvBatchTaskParameters
	{
		public string PlayerId;

		public object[] RawRow;
		public Action<string> Info { get; private set; }
		public Action<string> Error { get; private set; }
		public CsvWriter OutputWriter { get; private set; }


		public CsvBatchTaskParameters(string playerId, object[] rawRow, Action<string> info, Action<string> error,
									  CsvWriter outputWriter)
		{
			PlayerId = playerId;
			Info = info;
			Error = error;
			OutputWriter = outputWriter;
			RawRow = rawRow;
		}
	}


	public static int CountCsvRows(string file, bool header)
	{
		int rows = 0;
		using (var reader = new StreamReader(file))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				if (header)
				{
					csv.Read();
					csv.ReadHeader();
				}

				while (csv.Read())
				{
					rows++;
				}
			}

		return rows;
	}

	public static string[] GetCsvHeaders(this PlayfabScript script, string file)
	{
		using (var reader = new StreamReader(file))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				csv.Read();
				csv.ReadHeader();
				return csv.HeaderRecord;
			}
	}

	public static async IAsyncEnumerable<object[]> ReadFromCsv(this PlayfabScript script, string file,
															   bool header = true)
	{
		using (var reader = new StreamReader(file))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				if (header)
				{
					await csv.ReadAsync();
					csv.ReadHeader();
				}

				while (await csv.ReadAsync())
				{
					yield return csv.Parser.Record.ToArray();
				}
			}
	}

	public static async Task<string> BatchExecutionInCsv(this PlayfabScript script,
														 Func<CsvBatchTaskParameters, Task> processFunction,
														 string inputFileName,
														 string column,
														 bool header = true,
														 int maxTasks = 300)
	{
		var started = DateTime.Now;
		var processedPlayers = 0;
		var totalPlayers = CountCsvRows(inputFileName, header);
		var tasks = new List<Task>();
		var options = new ProgressBarOptions
		{
			ForegroundColor = ConsoleColor.Yellow,
			BackgroundColor = ConsoleColor.DarkGray,
			BackgroundCharacter = '\u2593',
			ShowEstimatedDuration = true,
		};
		int parsedIndex = 0;
		var usesColumnIndex = int.TryParse(column, out parsedIndex);

		Directory.CreateDirectory("output/");
		var outputFileName =
			Path.GetFullPath($"output/{script.GetType().Name.ToLower()}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.csv");
		Console.WriteLine("Output will be located at " + outputFileName);

		var totalBatches = totalPlayers / maxTasks;
		using (var streamWriter = new StreamWriter(outputFileName))
			using (var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
				   {
					   HasHeaderRecord = false
				   }))
			{
				using (var pbar = new ProgressBar(totalPlayers, "Reading file", options))
				{
					int fetchBatchCount = 1;

					int batch = 0;
					var playerIdCsvIndex = usesColumnIndex
						? parsedIndex
						: script.GetCsvHeaders(inputFileName).ToList().IndexOf(column);

					await foreach (var player in script.ReadFromCsv(inputFileName, header))
					{
						pbar.Message = "Processing batch " + batch + " of " + totalBatches;
						tasks.Add(processFunction(new CsvBatchTaskParameters(player[playerIdCsvIndex].ToString(),
								player,
								pbar.WriteLine,
								pbar.WriteErrorLine, csvWriter))
							.ContinueWith((t) =>
							{
								processedPlayers++;
								var timeSpanPerPlayer = (DateTime.Now - started) / processedPlayers;
								pbar?.Tick(estimatedDuration: (DateTime.Now - started) + (timeSpanPerPlayer *
									(totalPlayers - processedPlayers)));
							}));

						if (tasks.Count >= maxTasks)
						{
							await Task.WhenAll(tasks.ToArray());
							pbar.Message = "Waiting 1s!";
							await Task.Delay(1);
							tasks.Clear();
							batch++;
						}
					}


					// Ensure any remaining tasks in the list are awaited before completing the method
					if (tasks.Count > 0)
					{
						await Task.WhenAll(tasks.ToArray());
					}


					pbar.Message = "FINISHED!";
				}
			}

		Console.WriteLine("Output written at " + outputFileName);
		await Task.Delay(TimeSpan.FromSeconds(1));
		return outputFileName;
	}

	public static async Task<string> BatchExecutionInSegment(this PlayfabScript script,
															 Func<BatchTaskParams, Task> processFunction,
															 string segmentId = "all",
															 uint? maxFetchSize = 10000,
															 int maxTasks = 300
	)
	{
		segmentId = segmentId == "all" ? script.GetPlayfabConfiguration().AllPlayersSegmentId : segmentId;
		var started = DateTime.Now;
		var processedPlayers = 0;
		var totalPlayers = await script.GetPlayerAmountInSegment(segmentId);

		var tasks = new List<Task>();
		var options = new ProgressBarOptions
		{
			ForegroundColor = ConsoleColor.Yellow,
			BackgroundColor = ConsoleColor.DarkGray,
			BackgroundCharacter = '\u2593',
			ShowEstimatedDuration = true,
		};
		var childOptions = new ProgressBarOptions
		{
			ForegroundColor = ConsoleColor.Green,
			BackgroundColor = ConsoleColor.DarkGray,
			BackgroundCharacter = '\u2593',
			CollapseWhenFinished = true,
		};

		Directory.CreateDirectory("output/");
		var fetchBatchTotal = (int) Math.Ceiling((float) totalPlayers / (float) maxFetchSize);
		var fileName =
			Path.GetFullPath($"output/{script.GetType().Name.ToLower()}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.csv");
		Console.WriteLine("Output will be located at " + fileName);
		using (var streamWriter = new StreamWriter(fileName))
			using (var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
				   {
					   HasHeaderRecord = true
				   }))
			{
				using (var pbar = new ProgressBar(totalPlayers, "Fetching players", options))
				{
					int fetchBatchCount = 1;
					await foreach (var fetchBatch in script.GetPlayersEnumerable(segmentId, maxFetchSize))
					{
						int batch = 0;
						pbar.Message = "Processing batch " + fetchBatchCount + " of " + fetchBatchTotal;
						using (var child = pbar.Spawn(fetchBatch.Count, "Batch processing " + fetchBatchCount,
								   childOptions))
						{
							foreach (var player in fetchBatch)
							{
								child.Message = "Processing sub batch " + batch;
								tasks.Add(processFunction(new BatchTaskParams(player, pbar.WriteLine,
										pbar.WriteErrorLine, csvWriter))
									.ContinueWith((t) =>
									{
										processedPlayers++;
										var timeSpanPerPlayer = (DateTime.Now - started) / processedPlayers;
										pbar?.Tick(estimatedDuration: (DateTime.Now - started) + (timeSpanPerPlayer *
											(totalPlayers - processedPlayers)));
										child?.Tick();
									}));

								if (tasks.Count >= maxTasks)
								{
									await Task.WhenAll(tasks.ToArray());
									child.Message = "Waiting 1s!";
									await Task.Delay(1);
									tasks.Clear();
									batch++;
								}
							}

							fetchBatchCount++;
							pbar.Message = "Fetching players batch " + fetchBatchCount + " of " + fetchBatchTotal;
						}

						// Ensure any remaining tasks in the list are awaited before completing the method
						if (tasks.Count > 0)
						{
							await Task.WhenAll(tasks.ToArray());
						}
					}

					pbar.Message = "FINISHED!";
				}
			}

		Console.WriteLine("Output written at " + fileName);
		await Task.Delay(TimeSpan.FromSeconds(1));
		return fileName;
	}
}