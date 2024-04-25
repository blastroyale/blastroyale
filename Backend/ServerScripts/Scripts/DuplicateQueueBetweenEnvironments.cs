using System;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using Scripts.Base;


public class DuplicateQueueBetweenEnvironments : PlayfabScript
{
	public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.PROD;

	public override void Execute(ScriptParameters parameters)
	{
		RunAsync().Wait();
	}

	public async Task RunAsync()
	{
		var availableEnvironments = string.Join(", ", Enum.GetNames<PlayfabEnvironment>());
		Console.WriteLine("Available Environments: " + availableEnvironments);
		Console.WriteLine("Input the environment to copy from: ");
		var sourceEnv = Console.ReadLine();
		if (!Enum.TryParse<PlayfabEnvironment>(sourceEnv?.Trim(), out var source))
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
		var queueInput = Console.ReadLine();
		if (!availableQueues.Contains(queueInput))
		{
			Console.WriteLine("Queue not found!");
			return;
		}
		var sourceQueue = await PlayFabMultiplayerAPI.GetMatchmakingQueueAsync(new GetMatchmakingQueueRequest()
		{
			QueueName = queueInput
		}).HandleError();

		// Target
		Console.WriteLine();
		Console.WriteLine("Available Environments: " + availableEnvironments);
		Console.WriteLine("Input the environment to copy to: ");
		var targetString = Console.ReadLine();
		if (!Enum.TryParse<PlayfabEnvironment>(targetString?.Trim().ToUpperInvariant(), out var target))
		{
			Console.WriteLine("Invalid environment, available ones: " + availableEnvironments);
			return;
		}

		SetEnvironment(target);
		await AuthenticateServer();

		var existingQueue = await PlayFabMultiplayerAPI.GetMatchmakingQueueAsync(new GetMatchmakingQueueRequest()
		{
			QueueName = queueInput
		});
		if (existingQueue.Error == null || existingQueue.Error.Error != PlayFabErrorCode.MatchmakingQueueNotFound)
		{
			Console.WriteLine();
			Console.WriteLine("Queue already exists in target environment, do you want to overwrite it? (Y/N)");
			var shouldOverwrite = Console.ReadLine();
			if (!shouldOverwrite.Trim().ToLowerInvariant().Equals("y"))
			{
				Console.WriteLine("OK BYE BYE");
				return;
			}
		}

		await PlayFabMultiplayerAPI.SetMatchmakingQueueAsync(new SetMatchmakingQueueRequest()
		{
			MatchmakingQueue = sourceQueue.MatchmakingQueue
		}).HandleError();
		Console.WriteLine($"Copied queue {queueInput} from {source} to {target}!");
	}
}