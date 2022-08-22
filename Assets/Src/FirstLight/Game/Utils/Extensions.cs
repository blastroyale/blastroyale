using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Infos;
using FirstLight.Game.Input;
using FirstLight.Services;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

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
		/// Requests the localized text representing the given <paramref name="stat"/>
		/// </summary>
		public static string GetTranslation(this StatType stat)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.General)}/{stat.ToString()}");
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/>
		/// </summary>
		public static string GetTranslation(this GameId id)
		{
			return LocalizationManager.GetTranslation(id.GetTranslationTerm());
		}

		/// <summary>
		/// Requests the localized text representing the given <paramref name="stat"/>
		/// </summary>
		public static string GetTranslation(this EquipmentStatType stat)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.General)}/{stat.ToString()}");
		}

		/// <summary>
		/// Get's the translation term of the given <paramref name="id"/>
		/// </summary>
		public static string GetTranslationTerm(this GameId id)
		{
			return $"{nameof(ScriptTerms.GameIds)}/{id.ToString()}";
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="group"/>
		/// </summary>
		public static string GetTranslation(this GameIdGroup group)
		{
			return LocalizationManager.GetTranslation(group.GetTranslationTerm());
		}

		/// <summary>
		/// Get's the translation term of the given <paramref name="group"/>
		/// </summary>
		public static string GetTranslationTerm(this GameIdGroup group)
		{
			return $"{nameof(ScriptTerms.GameIds)}/{group.ToString()}";
		}

		/// <summary>
		/// Requests the localized text representing the ordinal of the given <paramref name="number"/>
		/// </summary>
		public static string GetOrdinalTranslation(this int number)
		{
			number = number > 19 ? number % 10 : number;
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.General)}/Ordinal{number.ToString()}");
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
		public static string ToHoursMinutesSeconds(this uint num)
		{
			var ts = TimeSpan.FromSeconds(num);

			if (ts.Hours > 0)
			{
				return string.Format("{0}h {1}m {2}s", ts.Hours.ToString(), ts.Minutes.ToString(),
				                     ts.Seconds.ToString());
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

			if (!groups.Contains(GameIdGroup.Equipment))
			{
				throw new
					ArgumentException($"The item {item} is not a {nameof(GameIdGroup.Equipment)} type to put in a slot");
			}

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
			if (data.Data.IsBot)
			{
				return GetBotName(data.PlayerName);
			}

			return data.PlayerName;
		}

		/// <summary>
		/// Requests the bot name for the given bot's <paramref name="nameIndex"/>
		/// </summary>
		public static string GetBotName(string nameIndex)
		{
			var term = ScriptTerms.BotNames.Bot1.Remove(ScriptTerms.BotNames.Bot1.Length - 1);

			return LocalizationManager.GetTranslation($"{term}{nameIndex}");
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
		/// Obtains the current selected map id in the given <paramref name="room"/>
		/// </summary>
		public static int GetMapId(this Room room)
		{
			return (int) room.CustomProperties[GameConstants.Network.ROOM_PROPS_MAP];
		}

		/// <summary>
		/// Obtains the current selected room code name in the given <paramref name="room"/>
		/// </summary>
		public static string GetRoomName(this Room room)
		{
			return room.Name.Split(NetworkUtils.ROOM_SEPARATOR)[0];
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
		public static bool IsRankedRoom(this Room room)
		{
			return (bool) room.CustomProperties[GameConstants.Network.ROOM_PROPS_RANKED_MATCH];
		}

		/// <summary>
		/// Obtains amount of non-spectator players currently in room
		/// </summary>
		public static int GetRealPlayerAmount(this Room room)
		{
			int playerAmount = 0;

			foreach (var kvp in room.Players)
			{
				var isSpectator = (bool) kvp.Value.CustomProperties[GameConstants.Network.PLAYER_PROPS_SPECTATOR];

				if (!isSpectator)
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
			return room.IsMatchmakingRoom() ? 0 : GameConstants.Data.MATCH_SPECTATOR_SPOTS;
		}
		
		/// <summary>
		/// Obtains info on whether room has all its player slots full
		/// </summary>
		public static bool IsAtFullPlayerCapacity(this Room room)
		{
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
		/// Requests the current state of the given <paramref name="room"/> if it is ready to start the game or not
		/// based on loading state of all players assets
		/// </summary>
		public static bool AreAllPlayersReady(this Room room)
		{
			foreach (var playerKvp in room.Players)
			{
				if (!playerKvp.Value.CustomProperties.TryGetValue(GameConstants.Network.PLAYER_PROPS_ALL_LOADED,
				                                                  out var propertyValue) ||
				    !(bool) propertyValue)
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
		/// Requests the <see cref="PlayerMatchData"/> of the current local player playing the game
		/// </summary>
		public static PlayerMatchData GetLocalPlayerData(this QuantumGame game, bool isVerified, out Frame f)
		{
			f = isVerified ? game.Frames.Verified : game.Frames.Predicted;
			
			return f.GetSingleton<GameContainer>().PlayersData[game.GetLocalPlayers()[0]];
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
	}
}