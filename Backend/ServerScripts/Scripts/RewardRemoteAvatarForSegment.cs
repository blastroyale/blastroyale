using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using PlayFab.ServerModels;
using Quantum;

namespace Scripts.Scripts;

/*
 * A Remote Avatar is a profile picture that doesn't have any reference/GameID,
 * this script can be used to distribute special avatar during events (i.e. Loot-2-Airdrop)
 */
public class RewardRemoteAvatarForSegment : PlayfabScript
{
	public override Environment GetEnvironment() => Environment.DEV;

	private const string REMOTE_AVATAR_URL = "https://blastroyalecdn.blob.core.windows.net/avatars/remote/noob.png";
	private const string SEGMENT_NAME = "Reward L2A Noob Avatar";

	private const bool SHOULD_TAG = true;
	private const string TAG_NAME = "loot-2-airdrop-noob_avatar";
	
	public override void Execute(ScriptParameters parameters)
	{
		if (string.IsNullOrEmpty(REMOTE_AVATAR_URL) || !IsValidUrl(REMOTE_AVATAR_URL))
		{
			throw new ArgumentException("Invalid REMOTE AVATAR URL, check REMOTE_AVATAR_URL value");
		}

		Console.WriteLine($"\nYou will add the following item to the collections of players in the '${SEGMENT_NAME}' segment:");
		Console.WriteLine($"	* RemoteAvatar with URL: {REMOTE_AVATAR_URL}");

		if (SHOULD_TAG)
		{
			Console.WriteLine($"\n After adding the item to the player's collection, the player will be tagged with the following tag: {TAG_NAME}");
		}

		Console.WriteLine("\n Do you want to proceed?  y/n");
		
		var option = Console.ReadLine();

		switch (option?.ToLower())
		{
			case "y":
				Init();
				break;
			
			case "n":
				return;
			
			default:
				Console.WriteLine("Invalid Option!");
				break;
		}
		
	}

	private void Init()
	{
		
		var task = RunAsync();
		task.Wait();
	}

	public async Task RunAsync()
	{
		var tasks = new List<Task>();
		var batchSize = 10000;
		var playerList = await GetPlayerSegmentByName(SEGMENT_NAME);
		
		Console.WriteLine($"Total Players to Process: {playerList.Count}");
		var processedPlayers = 0;
		
		foreach (var player in playerList)
		{
			tasks.Add(Process(player));
			processedPlayers++;

			if (tasks.Count >= batchSize)
			{
				Console.WriteLine($"Processed Players: {processedPlayers}/{playerList.Count}");
				await Task.WhenAll(tasks.ToArray());
				
				Console.WriteLine("Waiting for 1 second before next batch...");

				
				await Task.Delay(1000);
				tasks.Clear(); 
			}
		}

		// Ensure any remaining tasks in the list are awaited before completing the method
		if (tasks.Count > 0)
		{
			Console.WriteLine($"Processing final batch, Processed Players: {processedPlayers}/{playerList.Count}");
			await Task.WhenAll(tasks.ToArray());
		}
			
		Console.WriteLine("All batches processed. Done!");
	}
	

	private async Task Process(PlayerProfile profile)
	{
		var state = await ReadUserState(profile.PlayerId);

		if (state != null)
		{
			var collectionData = state.DeserializeModel<CollectionData>();
			var avatars = collectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PROFILE_PICTURE, out var playerAvatar) ? playerAvatar : new List<ItemData>();

			var remoteAvatar = CreateRemoteAvatar();

			if (!avatars.Contains(remoteAvatar))
			{
				avatars.Add(remoteAvatar);
				collectionData.OwnedCollectibles[CollectionCategories.PROFILE_PICTURE] = avatars;
				
				state.UpdateModel(collectionData);
				await SetUserState(profile.PlayerId, state);			
			}
			else
			{
				if (SHOULD_TAG)
				{
					await TagPlayer(profile, TAG_NAME);
				}
			}
		}
		else
		{
			Console.WriteLine($"Something went wrong when reading UserState for user {profile.PlayerId}");
			
		}
	}

	private ItemData CreateRemoteAvatar()
	{
		return ItemFactory.Collection(GameId.AvatarRemote, new CollectionTrait[]
		{
			new (CollectionTraits.URL, REMOTE_AVATAR_URL)
		});
	}
	
	private static bool IsValidUrl(string url)
	{
		if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
		{
			return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
		}
		return false;
	}
}