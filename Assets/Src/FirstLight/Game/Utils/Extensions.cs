using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Input;
using FirstLight.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Modules;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using EventBase = Quantum.EventBase;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using PlayerMatchData = Quantum.PlayerMatchData;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This class has a list of useful extensions to be used in the project
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Requests the hierarchy path in the scene of the given game object
		/// </summary>
		public static string FullGameObjectPath(this GameObject go)
		{
			var name = go.name;
			while (go.transform.parent)
			{
				go = go.transform.parent.gameObject;
				name = $"{go.name}.{name}";
			}

			return name;
		}

		/// <summary>
		/// Strips the X amount of playfab generated numbers that are appended to the player name
		/// </summary>
		public static string TrimPlayerNameNumbers(this string playerName)
		{
			int appendedNumberAmount = GameConstants.Data.PLAYER_NAME_APPENDED_NUMBERS;
			return playerName.Remove(playerName.Length - appendedNumberAmount, appendedNumberAmount);
		}


		/// <summary>
		/// Get Photon region translation for the given <paramref name="regionKey"/> 
		/// </summary>
		public static string GetPhotonRegionTranslation(this string regionKey)
		{
			switch (regionKey)
			{
				case "eu":
					return ScriptLocalization.MainMenu.ServerNameEu;
				case "us":
					return ScriptLocalization.MainMenu.ServerNameUs;
				case "hk":
				case "asia":
				case "in":
					return ScriptLocalization.MainMenu.ServerNameHk;
				default:
					return "";
			}
		}

		/// <summary>
		/// Sets the Layer of a game object and if include children is set to true, also sets all child objects to that layer too.
		/// </summary>
		public static void SetLayer(this GameObject parent, int layer, bool includeChildren = true)
		{
			parent.layer = layer;
			if (includeChildren)
			{
				foreach (Transform trans in parent.transform.GetComponentsInChildren<Transform>(true))
				{
					trans.gameObject.layer = layer;
				}
			}
		}

		/// <summary>
		/// Validates the given <paramref name="component"/> to check if is not yet destroyed.
		/// This is just sugar syntax to improve the code readability.
		/// </summary>
		public static T Validate<T>(this Component component) where T : Component
		{
			return component.IsDestroyed() ? null : component as T;
		}

		/// <summary>
		/// Checks if the given <paramref name="component"/> is destroyed.
		/// This is just sugar syntax to improve the code readability.
		/// </summary>
		public static bool IsDestroyed(this Component component)
		{
			return component == null || component.Equals(null) || component.gameObject == null;
		}

		/// <summary>
		/// Calls the given <paramref name="onCallback"/> after the given <paramref name="duration"/> is completed and
		/// only if the given <paramref name="component"/> is still alive
		/// </summary>
		public static async void LateCall(this Component component, float duration, Action onCallback)
		{
			await LateCallAwaitable(component, duration, onCallback);
		}

		/// <summary>
		/// Uses a coroutine to call the given <paramref name="onCallback"/> after the given <paramref name="duration"/> is completed and
		/// only if the given <paramref name="component"/> is still alive
		/// </summary>
		/// <remarks>
		/// Coroutines only get executed if the game object is active and alive. Use <see cref="LateCall"/> for other purposes.
		/// </remarks>
		public static void LateCoroutineCall(this MonoBehaviour component, float duration, Action onCallback)
		{
			component.StartCoroutine(DelayCoroutine(duration, onCallback));

			IEnumerator DelayCoroutine(float time, Action callback)
			{
				yield return new WaitForSeconds(time);

				callback?.Invoke();
			}
		}

		/// <inheritdoc cref="LateCall"/>
		/// <remarks>
		/// Extends the method to allow awaitable task callbacks
		/// </remarks>
		public static async Task LateCallAwaitable(this Component component, float duration, Action onCallback)
		{
			await Task.Delay((int) (duration * 1000));

			if (!component.IsDestroyed())
			{
				onCallback?.Invoke();
			}
		}

		/// <summary>
		/// Formats a large number in the decimal format to use K, M or B. E.g. 9800 = 9.8K.
		/// </summary>
		public static string ToKMB(this uint num)
		{
			if (num > 999999999)
			{
				return num.ToString("0,,,.###B", CultureInfo.InvariantCulture);
			}
			else if (num > 999999)
			{
				return num.ToString("0,,.##M", CultureInfo.InvariantCulture);
			}
			else if (num > 999)
			{
				return num.ToString("0,.#K", CultureInfo.InvariantCulture);
			}
			else
			{
				return num.ToString(CultureInfo.InvariantCulture);
			}
		}

		/// <summary>
		/// Formats a string in seconds to H M S format. E.g. shows a chest has 3H to unlock.
		/// </summary>
		public static string ToHMS(this uint num)
		{
			var ts = TimeSpan.FromSeconds(num);

			if (ts.Hours > 0)
			{
				if (ts.Minutes > 0)
				{
					return $"{ts.TotalHours.ToString()}h ${ts.Minutes.ToString()}m";
				}

				return $"{ts.TotalHours.ToString()}h";
			}

			if (ts.Minutes > 0)
			{
				return $"{ts.Minutes.ToString()}m";
			}

			return $"{ts.Seconds.ToString()}s";
		}

		/// <summary>
		/// Formats a string in seconds to Hours and Minutes and Seconds.
		/// </summary>
		public static string ToHoursMinutesSeconds(this TimeSpan ts)
		{
			if (ts.Hours > 0)
			{
				return string.Format("{0}h {1}m", ts.Hours.ToString(), ts.Minutes.ToString());
			}

			if (ts.Minutes > 0)
			{
				return string.Format("{0}m {1}s", ts.Minutes.ToString(), ts.Seconds.ToString());
			}

			return string.Format("{0}s", ts.Seconds.ToString());
		}

		/// <summary>
		/// Requests the <see cref="GameIdGroup"/> slot representing the given <see cref="item"/>
		/// </summary>
		public static GameIdGroup GetSlot(this GameId item)
		{
			var groups = item.GetGroups();

			return groups[0];
		}

		/// <summary>
		/// Requests the alive/dead status of the player entity (exists? alive?)
		/// </summary>
		public static bool IsAlive(this EntityRef entity, Frame f)
		{
			if (!f.TryGet<Stats>(entity, out var stats))
			{
				return false;
			}

			return stats.CurrentHealth > 0;
		}

		/// <summary>
		/// Requests the player name for the given player's match <paramref name="data"/>
		/// </summary>
		public static string GetPlayerName(this QuantumPlayerMatchData data)
		{
			if (data.IsBot)
			{
				return GetBotName(data.Data.BotNameIndex, data.Data.Entity);
			}

			return data.PlayerName;
		}
		
		/// <summary>
		/// Requests the player name for the given player's match <paramref name="data"/>
		/// </summary>
		public static string GetPlayerName(Frame f, EntityRef entity, PlayerCharacter playerCharacter)
		{
			return !playerCharacter.RealPlayer && f.TryGet<BotCharacter>(entity, out var botCharacter)
				? GetBotName(botCharacter.BotNameIndex, entity)
				: f.GetPlayerData(playerCharacter.Player).PlayerName;
		}

		public static bool IsBot(this EntityRef entity, Frame f)
		{
			return f.Has<BotCharacter>(entity);
		}

		/// <summary>
		/// Requests the bot name for the given bot. In debug build this would
		/// display "BOT-e(entity_index)-b(behaviour_index).
		/// </summary>
		public static string GetBotName(int nameIndex, EntityRef entityRef)
		{
			if (Debug.isDebugBuild)
			{
				return $"BOT-{entityRef.Index}";
			}

			var term = ScriptTerms.BotNames.Bot1.Remove(ScriptTerms.BotNames.Bot1.Length - 1);

			return LocalizationManager.GetTranslation($"{term}{nameIndex.ToString()}");
		}

		/// <summary>
		/// Makes the timelines <see cref="PlayableGraph"/> resume it's current play
		/// </summary>
		public static void PlayTimeline(this PlayableGraph graph)
		{
			graph.GetRootPlayable(0).SetSpeed(1);
			graph.Play();
		}

		/// <summary>
		/// Makes the timelines <see cref="PlayableGraph"/> stop it's current play
		/// </summary>
		public static void StopTimeline(this PlayableGraph graph)
		{
			graph.Stop();
			graph.GetRootPlayable(0).SetSpeed(0);
		}

		/// <summary>
		/// Requests the Verified state of the current <see cref="Frame"/> that triggered the given <paramref name="eventBase"/>.
		/// Returns TRUE if this frame was verified by all running clients, FALSE otherwise
		/// </summary>
		public static bool IsVerifiedFrame(this EventBase eventBase)
		{
			return eventBase.Game.Session.IsFrameVerified(eventBase.Tick);
		}

		/// <summary>
		/// Returns true if the given <paramref name="room"/> is a playtest room
		/// </summary>
		public static bool IsPlayTestRoom(this Room room)
		{
			return room.Name.Contains(GameConstants.Network.ROOM_NAME_PLAYTEST);
		}

		/// <summary>
		/// Returns true if the given <paramref name="roomName"/> is a playtest room
		/// </summary>
		public static bool IsPlayTestRoom(this string roomName)
		{
			return roomName.Contains(GameConstants.Network.ROOM_NAME_PLAYTEST);
		}

		/// <summary>
		/// Obtains the current selected map id in the given <paramref name="room"/>
		/// </summary>
		public static int GetMapId(this Room room)
		{
			return (int) room.CustomProperties[GameConstants.Network.ROOM_PROPS_MAP];
		}
		
		public static List<GameId> GetLoadoutGameIds(this Player player)
		{
			return ((int[])player.CustomProperties[GameConstants.Network.PLAYER_PROPS_LOADOUT]).Cast<GameId>().ToList();
		}

		/// <summary>
		/// Obtains the current selected game mode id in the given <paramref name="room"/>
		/// </summary>
		public static string GetGameModeId(this Room room)
		{
			return (string) room.CustomProperties[GameConstants.Network.ROOM_PROPS_GAME_MODE];
		}

		/// <summary>
		/// Return if this room was created by playfab matchmaking
		/// </summary>
		public static bool ShouldUsePlayFabMatchmaking(this Room room, IConfigsProvider configsProvider)
		{
			var gamemodeId = room.GetGameModeId();
			return configsProvider.GetConfig<QuantumGameModeConfig>(gamemodeId).ShouldUsePlayfabMatchmaking();
		}


		/// <summary>
		/// Obtains the current room creation time (created with UTC.Now)
		/// </summary>
		public static DateTime GetRoomCreationDateTime(this Room room)
		{
			return new DateTime((long) room.CustomProperties[GameConstants.Network.ROOM_PROPS_CREATION_TICKS]);
		}

		/// <summary>
		/// Obtains the current dropzone pos+rot vector3 for the given <paramref name="room"/>
		/// </summary>
		public static Vector3 GetDropzonePosRot(this Room room)
		{
			return (Vector3) room.CustomProperties[GameConstants.Network.DROP_ZONE_POS_ROT];
		}

		/// <summary>
		/// Obtains the current room creation time (created with UTC.Now)
		/// </summary>
		public static string TrimRoomCommitLock(this string roomName)
		{
			return roomName.Replace(NetworkUtils.RoomCommitLockData, "");
		}

		/// <summary>
		/// Obtains the list of mutators enabled in the given <paramref name="room"/>
		/// </summary>
		public static List<string> GetMutatorIds(this Room room)
		{
			var str = (string) room.CustomProperties[GameConstants.Network.ROOM_PROPS_MUTATORS];
			return str.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
		}

		/// <summary>
		/// Obtains the current selected room code name in the given <paramref name="room"/>
		/// </summary>
		public static string GetRoomName(this Room room)
		{
			return room.Name.Split(GameConstants.Network.ROOM_META_SEPARATOR)[0];
		}

		/// <summary>
		/// Obtains info on whether the room is used for matchmaking
		/// </summary>
		public static bool IsMatchmakingRoom(this Room room)
		{
			return room.IsVisible;
		}

		/// <summary>
		/// Obtains info on whether the room is used for matchmaking
		/// </summary>
		public static bool HaveStartedGame(this Room room)
		{
			return room.GetProp<bool>(GameConstants.Network.ROOM_PROPS_STARTED_GAME);
		}

		/// <summary>
		/// Obtains the <see cref="MatchType"/> of this room.
		/// </summary>
		public static MatchType GetMatchType(this Room room)
		{
			return Enum.Parse<MatchType>((string) room.CustomProperties[GameConstants.Network.ROOM_PROPS_MATCH_TYPE]);
		}

		/// <summary>
		/// Can a game room frame be restored from a local snapshot ?
		/// </summary>
		public static bool CanBeRestoredWithLocalSnapshot(this Room room)
		{
			if (!MainInstaller.TryResolve<IGameServices>(out var _services))
			{
				return false;
			}

			return room.IsOffline;
		}

		/// <summary>
		/// Can a local snapshot with current simulation frame be saved locally to be restored later ?
		/// </summary>
		public static bool CanBeRestoredWithLocalSnapshot(this FrameSnapshot snapshot)
		{
			if (!MainInstaller.TryResolve<IGameServices>(out var _services))
			{
				return false;
			}

			return snapshot.Offline;
		}

		public static void SetProperty(this Room room, string prop, object value)
		{
			var table = new Hashtable();
			table[prop] = value;
			room.SetCustomProperties(table);
		}

		public static T GetProp<T>(this Room room, string prop)
		{
			if (room.CustomProperties.TryGetValue(prop, out var v))
				return (T) v;
			return default;
		}

		public static MatchRoomSetup GetMatchSetup(this Room room)
		{
			var str = (string) room.CustomProperties[GameConstants.Network.ROOM_PROPS_SETUP];
			return ModelSerializer.Deserialize<MatchRoomSetup>(str);
		}

		/// <summary>
		/// Obtains amount of non-spectator players currently in room
		/// </summary>
		public static int GetRealPlayerAmount(this Room room)
		{
			int playerAmount = 0;

			foreach (var kvp in room.Players)
			{
				kvp.Value.CustomProperties.TryGetValue(GameConstants.Network.PLAYER_PROPS_SPECTATOR, out var isSpectator);
				if (isSpectator is null or false)
				{
					playerAmount++;
				}
			}

			return playerAmount;
		}

		/// <summary>
		/// Obtains amount of spectators players currently in room
		/// </summary>
		public static int GetSpectatorAmount(this Room room)
		{
			int playerAmount = 0;

			foreach (var kvp in room.Players)
			{
				var isSpectator = (bool) kvp.Value.CustomProperties[GameConstants.Network.PLAYER_PROPS_SPECTATOR];

				if (isSpectator)
				{
					playerAmount++;
				}
			}

			return playerAmount;
		}

		/// <summary>
		/// Obtains room capacity for non-spectator players
		/// </summary>
		public static int GetRealPlayerCapacity(this Room room)
		{
			return room.MaxPlayers - room.GetSpectatorCapacity();
		}

		/// <summary>
		/// Obtains room capacity for non-spectator players
		/// </summary>
		public static int GetSpectatorCapacity(this Room room)
		{
			return NetworkUtils.GetMaxSpectators(room.GetMatchSetup());
		}

		/// <summary>
		/// Obtains info on whether room has all its player slots full
		/// </summary>
		public static bool IsAtFullPlayerCapacity(this Room room, IConfigsProvider cfgProvider)
		{
			// This is playfab mm
			if (room.ShouldUsePlayFabMatchmaking(cfgProvider) && room.ExpectedUsers != null && room.ExpectedUsers.Length > 0)
			{
				bool everyBodyJoined = room.ExpectedUsers
					.All(id => room.Players.Any(p => p.Value.UserId == id));

				bool everybodyLoadedCoreAssets = room.Players.Values.All(p => p.LoadedCoreMatchAssets());
				return everyBodyJoined && everybodyLoadedCoreAssets;
			}
			return room.GetRealPlayerAmount() >= room.GetRealPlayerCapacity();
		}

		/// <summary>
		/// Obtains info on whether room has all its spectator slots full
		/// </summary>
		public static bool IsAtFullSpectatorCapacity(this Room room)
		{
			return room.GetSpectatorAmount() >= room.GetSpectatorCapacity();
		}

		/// <summary>
		/// Obtains spectator/player status for player
		/// </summary>
		/// <returns></returns>
		public static bool IsSpectator(this Player player)
		{
			return (bool) player.CustomProperties[GameConstants.Network.PLAYER_PROPS_SPECTATOR];
		}

		/// <summary>
		/// Requests the team id of the player (-1 for no team).
		/// </summary>
		public static string GetTeamId(this Player player)
		{
			if (player.CustomProperties.TryGetValue(GameConstants.Network.PLAYER_PROPS_TEAM_ID, out var teamId))
			{
				return (string) teamId;
			}

			return string.Empty;
		}

		/// <summary>
		/// Requests the team id of the player (-1 for no team).
		/// </summary>
		public static Vector2 GetDropPosition(this Player player)
		{
			if (player.CustomProperties.TryGetValue(GameConstants.Network.PLAYER_PROPS_DROP_POSITION, out var dropPosition))
			{
				return (Vector2) dropPosition;
			}

			return Vector2.zero;
		}

		/// <summary>
		/// Requests to check if player has loaded core match assets
		/// </summary>
		public static bool LoadedCoreMatchAssets(this Player player)
		{
			return player.CustomProperties.TryGetValue(GameConstants.Network.PLAYER_PROPS_CORE_LOADED,
				out var propertyValue) && (bool)propertyValue;
		}

		/// <summary>
		/// Requests the current state of the given <paramref name="room"/> if it is ready to start the game or not
		/// based on loading state of all players assets
		/// </summary>
		public static bool AreAllPlayersReady(this Room room)
		{
			foreach (var playerKvp in room.Players)
			{
				// We check userid null because that means player is joining first time
				// if userid is not null means he entered the room then left, in this case room should start without him
				// with the player being inactive so he can join later
				if (playerKvp.Value.IsInactive && playerKvp.Value.UserId == null)
				{
					FLog.Verbose("Inactive player" + playerKvp.Value.LoadedCoreMatchAssets());
					continue;
				}
				
				if (!playerKvp.Value.LoadedCoreMatchAssets())
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Copy properties from one model to another.
		/// Only a shallow copy.
		/// </summary>
		public static void CopyPropertiesShallowTo<T>(this T source, T dest)
		{
			var a = dest.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var property in a)
			{
				property.SetValue(dest, property.GetValue(source));
			}
		}

		/// <summary>
		/// Requests the <see cref="Quantum.PlayerMatchData"/> of the current local player playing the game
		/// </summary>
		public static PlayerMatchData GetLocalPlayerData(this QuantumGame game, bool isVerified, out Frame f)
		{
			var localPlayers = game.GetLocalPlayers();

			f = isVerified ? game.Frames.Verified : game.Frames.Predicted;

			return localPlayers.Length == 0 ? new PlayerMatchData() : f.GetSingleton<GameContainer>().PlayersData[game.GetLocalPlayers()[0]];
		}

		/// <summary>
		/// Requests the <see cref="PlayerRef"/> of the current local player playing the game.
		/// If there is no local player in the match (ex: spectator in the match), returns <see cref="PlayerRef.None"/>
		/// </summary>
		public static PlayerRef GetLocalPlayerRef(this QuantumGame game)
		{
			var localPlayers = game.GetLocalPlayers();

			return localPlayers.Length == 0 ? PlayerRef.None : localPlayers[0];
		}
		
		/// <summary>
		/// Requests the local player entity ref.
		/// Always gets from Verified frame
		/// </summary>
		public static EntityRef GetLocalPlayerEntityRef(this QuantumGame game)
		{
			return game.GetLocalPlayerData(true, out _).Entity;
		}

		/// <summary>
		/// Requests the <see cref="InputAction"/> that controls the input for the special in the given <paramref name="index"/>
		/// </summary>
		public static InputAction GetSpecialButton(this LocalInput.GameplayActions gameplayActions, int index)
		{
			if (index == 0)
			{
				return gameplayActions.SpecialButton0;
			}

			return gameplayActions.SpecialButton1;
		}

		/// <summary>
		/// Sets the Display style property of an element.
		/// </summary>
		public static void SetDisplay(this VisualElement element, bool active)
		{
			// Enabling the class means that the element will become hidden
			element.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// Sets the Visibility style property of an element.
		/// </summary>
		public static void SetVisibility(this VisualElement element, bool visible)
		{
			element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
		}

		/// <summary>
		/// Iterates over a list by chunks
		/// </summary>
		public static IEnumerable<IList<T>> ChunksOf<T>(this IEnumerable<T> sequence, int size)
		{
			List<T> chunk = new List<T>(size);
			foreach (T element in sequence)
			{
				chunk.Add(element);
				if (chunk.Count == size)
				{
					yield return chunk;
					chunk = new List<T>(size);
				}
			}

			yield return chunk;
		}

		/// <summary>
		/// Check if the player have NFTs
		/// </summary>
		/// <param name="equipmentLogic"></param>
		[Obsolete("Please use iGameLogic.HasNfts")]
		public static bool HasNfts(this IEquipmentDataProvider equipmentLogic)
		{
			return MainInstaller.Resolve<IGameDataProvider>().HasNfts();
		}
		
		/// <summary>
		/// Check if the player have NFTs
		/// </summary>
		/// <param name="equipmentLogic"></param>

		public static bool HasNfts(this IGameDataProvider data)
		{
#if  UNITY_EDITOR || DEVELOPMENT_BUILD
			if (FeatureFlags.GetLocalConfiguration().ForceHasNfts)
			{
				return true;
			}
#endif
			var profilePictures =
				data.CollectionDataProvider.GetOwnedCollection(CollectionCategories.PROFILE_PICTURE);
			return profilePictures.Count > 0 || data.EquipmentDataProvider.NftInventory.Count > 0;
		}


		public static void SelectDefaultRankedMode(this IGameModeService service)
		{
			var gameMode = service.Slots.ReadOnlyList.FirstOrDefault(x => x.Entry.MatchType == MatchType.Ranked);
			service.SelectedGameMode.Value = gameMode;
		}

		public static AudioId GetAmbientAudioId(this AmbienceType ambience)
		{
			switch(ambience)
			{
				case AmbienceType.CityCenter:
					return AudioId.CentralAmbientLoop;
				
				case AmbienceType.Desert:
					return AudioId.DesertAmbientLoop;
				
				case AmbienceType.Forest:
					return AudioId.ForestAmbientLoop;
				
				case AmbienceType.Frost:
					return AudioId.FrostAmbientLoop;
				
				case AmbienceType.Lava:
					return AudioId.LavaAmbientLoop;
				
				case AmbienceType.Urban:
					return AudioId.UrbanAmbientLoop;

				case AmbienceType.Water:
					return AudioId.WaterAmbientLoop;
				
				default:
					throw new ArgumentOutOfRangeException(nameof(ambience), ambience, null);
			}
		}

		/// <summary>
		/// Checks if the entity is the player we are currently spectating.
		/// </summary>
		public static bool IsSpectatingPlayer(this IMatchServices matchServices, EntityRef entityRef)
		{
			return entityRef == matchServices.SpectateService.SpectatedPlayer.Value.Entity;
		}

		/// <summary>
		/// Returns the player we are currently spectating.
		/// </summary>
		public static SpectatedPlayer GetSpectatedPlayer(this IMatchServices matchServices)
		{
			return matchServices.SpectateService.SpectatedPlayer.Value;
		}
	}
}