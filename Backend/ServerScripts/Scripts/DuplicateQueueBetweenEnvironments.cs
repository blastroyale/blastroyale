using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using Scripts.Base;


public class DuplicateQueueBetweenEnvironments : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.DEV;

	public override void Execute(ScriptParameters parameters)
	{
		RunAsync().Wait();
	}

	public async Task RunAsync()
	{
		var availableEnvironments = string.Join(", ", Enum.GetNames<Environment>());
		Console.WriteLine("Available Environments: " + availableEnvironments);
		Console.WriteLine("Input the environment to copy from: ");
		var sourceEnv = Console.ReadLine();
		if (!Enum.TryParse<Environment>(sourceEnv?.Trim().ToUpperInvariant(), out var source))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}

		SetEnvironment(source);
		await AuthenticateServer();

		var listQueues = await PlayFabMultiplayerAPI.ListMatchmakingQueuesAsync(new ListMatchmakingQueuesRequest()).HandleError();

		Console.WriteLine();
		Console.WriteLine("Available queues to copy from:");
		var availableQueues = listQueues.MatchMakingQueues.Select(existingQueue => existingQueue.Name).ToList();
		listQueues.MatchMakingQueues.Select(existingQueue => $"Name: {existingQueue.Name}, MaxPlayers: {existingQueue.MaxMatchSize}, MaxTicketSize: {existingQueue.MaxTicketSize}")
			.ToList()
			.ForEach(Console.WriteLine);
		Console.WriteLine();
		Console.WriteLine("Input the queue name (you can input multiple with commas): ");
		var input = Console.ReadLine();
		var inputQueues = input.Split(",");
		var sourceQueues = new Dictionary<string, GetMatchmakingQueueResult>();
		// Validate all
		foreach (var inputQueueName in inputQueues)
		{
			var trimmedQueue = inputQueueName.Trim();
			if (!availableQueues.Contains(trimmedQueue))
			{
				Console.WriteLine($"Queue {trimmedQueue} not found!");
				return;
			}

			var sourceQueue = await PlayFabMultiplayerAPI.GetMatchmakingQueueAsync(new GetMatchmakingQueueRequest()
			{
				QueueName = trimmedQueue
			}).HandleError();
			sourceQueues[trimmedQueue] = sourceQueue;
		}

		// Target
		Console.WriteLine();
		Console.WriteLine("Available Environments: " + availableEnvironments);
		Console.WriteLine("Input the environment to copy to: ");
		var targetString = Console.ReadLine();
		if (!Enum.TryParse<Environment>(targetString?.Trim().ToUpperInvariant(), out var target))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}

		SetEnvironment(target);
		await AuthenticateServer();

		foreach (var (queueName, value) in sourceQueues)
		{
			var existingQueue = await PlayFabMultiplayerAPI.GetMatchmakingQueueAsync(new GetMatchmakingQueueRequest()
			{
				QueueName = queueName
			});
			if (existingQueue.Error == null || existingQueue.Error.Error != PlayFabErrorCode.MatchmakingQueueNotFound)
			{
				Console.WriteLine();
				Console.WriteLine($"Queue {queueName} already exists in target environment, do you want to overwrite it? (Y/N)");
				var shouldOverwrite = Console.ReadLine();
				if (!shouldOverwrite.Trim().ToLowerInvariant().Equals("y"))
				{
					Console.WriteLine("OK BYE BYE");
					return;
				}
			}

			await PlayFabMultiplayerAPI.SetMatchmakingQueueAsync(new SetMatchmakingQueueRequest()
			{
				MatchmakingQueue = value.MatchmakingQueue
			}).HandleError();
			Console.WriteLine($"Copied queue {queueName} from {source} to {target}!");
		}
	}
}