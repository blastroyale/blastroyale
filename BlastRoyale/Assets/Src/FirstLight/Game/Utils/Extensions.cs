using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Ids;
using FirstLight.Game.Input;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using PlayerMatchData = Quantum.PlayerMatchData;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This class has a list of useful extensions to be used in the project
	/// </summary>
	public static class Extensions
	{
		private static PlayerMatchData _defaultPlayerMatchDataReference = default;

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
			int index = playerName.LastIndexOf("#", StringComparison.Ordinal);
			if (index == -1) return playerName;
			return playerName.Substring(0, index);
		}

		/// <summary>
		/// Get Photon region translation for the given <paramref name="regionKey"/> 
		/// </summary>
		public static string GetPhotonRegionTranslation(this string regionKey)
		{
			if (LocalizationManager.TryGetTranslation("UITSettings/region_" + regionKey.ToLowerInvariant(), out var translation))
			{
				return translation;
			}

			switch (regionKey)
			{
				case "eu":
					return ScriptLocalization.UITSettings.region_eu;
				case "us":
					return ScriptLocalization.UITSettings.region_us;
				case "hk":
					return ScriptLocalization.UITSettings.region_hk;
				case "asia":
					return ScriptLocalization.UITSettings.region_asia;
				case "in":
					return ScriptLocalization.UITSettings.region_in;
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
		public static async UniTaskVoid LateCall(this Component component, float duration, Action onCallback)
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
		public static async UniTask LateCallAwaitable(this Component component, float duration, Action onCallback)
		{
			await UniTask.Delay((int) (duration * 1000));

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
		/// Formats a string in seconds to Days and Hours.
		/// </summary>
		public static string ToDayAndHours(this TimeSpan ts, bool simplified = false)
		{
			if (ts.Days > 0)
			{
				return simplified
					? $"{ts.Days.ToString()}d {ts.Hours.ToString()}h"
					: $"{ts.Days.ToString()} days and {ts.Hours.ToString()} hours";
			}

			return ts.Hours + (simplified ? "h" : " hours");
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
			if (!entity.IsValid || !f.TryGet<Stats>(entity, out var stats))
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
		/// Obtains the current selected room code name in the given <paramref name="room"/>
		/// </summary>
		public static string GetRoomName(this Room room)
		{
			return room.Name.Split(GameConstants.Network.ROOM_META_SEPARATOR)[0];
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
		public static unsafe ref PlayerMatchData GetLocalPlayerData(this QuantumGame game, bool isVerified, out Frame f)
		{
			f = isVerified ? game.Frames.Verified : game.Frames.Predicted;
			if (game.Frames.Verified == null || game.IsSessionDestroyed)
			{
				FLog.Warn("Trying to access simulation data without simulation running.");
				return ref _defaultPlayerMatchDataReference;
			}
			var localPlayers = game.GetLocalPlayers();
			if (localPlayers.Length == 0) return ref _defaultPlayerMatchDataReference;
			return ref *f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData.GetPointer(localPlayers[0]);
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
		public static EntityRef GetLocalPlayerEntityRef(this QuantumGame game, bool isVerified = true)
		{
			return game.GetLocalPlayerData(isVerified, out _).Entity;
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
		public static VisualElement SetDisplay(this VisualElement element, bool active)
		{
			// Enabling the class means that the element will become hidden
			element.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
			return element;
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

		public static void SelectDefaultRankedMode(this IGameModeService service)
		{
			var gameMode = service.Slots.FirstOrDefault(x => x.Entry is {MatchConfig: not null} and FixedGameModeEntry);
			service.SelectedGameMode.Value = gameMode;
		}

		public static AudioId GetAmbientAudioId(this AmbienceType ambience)
		{
			switch (ambience)
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

		public static T CloneSerializing<T>(this T value)
		{
			return ModelSerializer.Deserialize<T>(ModelSerializer.Serialize(value).Value);
		}

		public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
		{
			return source.OrderBy((_) => Random.value);
		}

		public static T RandomElement<T>(this IList<T> source)
		{
			return source[Random.Range(0, source.Count)];
		}
	}
}