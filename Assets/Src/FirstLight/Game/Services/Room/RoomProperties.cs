using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using Quantum;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Services.RoomService
{
	public class RoomProperties
	{
		private List<IRoomProperty> _allProperties;

		// The commit should guarantee the same Quantum build version + App version etc.
		public RoomProperty<string> Commit;

		// A list of mutators used in this room
		public RoomProperty<List<string>> Mutators;
		public RoomProperty<bool> HasBots;
		public RoomProperty<List<GameId>> AllowedRewards;
		public RoomProperty<bool> GameStarted;
		public RoomProperty<bool> StartCustomGame;
		public RoomProperty<int> LoadingStartServerTime;
		public RoomProperty<int> SecondsToStart;
		public RoomProperty<int> BotDifficultyOverwrite;

		// For matchmaking, rooms are segregated by casual/ranked.
		public RoomProperty<MatchType> MatchType;

		// For matchmaking, rooms are segregated by casual/ranked.
		public RoomProperty<string> GameModeId;

		// Set the game map Id for the same matchmaking
		public RoomProperty<GameId> MapId;

		public delegate void OnSetPropertyCallback(string key, object value);

		public event OnSetPropertyCallback OnLocalPlayerSetProperty;

		public RoomProperties()
		{
			// keys here do not matter, it should be short and unique.
			// all properties should be access through this object
			_allProperties = new List<IRoomProperty>();
			Commit = Create<string>("cm", true);
			Mutators = CreateList("mttrs", true);
			MatchType = CreateEnum<MatchType>("mt", true);
			AllowedRewards = CreateEnumList<GameId>("alrewards", true);
			GameModeId = Create<string>("gmid", true);
			MapId = CreateEnum<GameId>("mapid", true);
			GameStarted = Create<bool>("started",true);
			HasBots = Create<bool>("bots");
			SecondsToStart = Create<int>("secondstostart");
			LoadingStartServerTime = Create<int>("loading");
			BotDifficultyOverwrite = Create<int>("botdif");
			StartCustomGame = Create<bool>("cstart");
		}
		

		private void InitProperty<T>(RoomProperty<T> property)
		{
			_allProperties.Add(property);
			property.OnLocalPlayerSet += OnLocalPlayerSetPropertyCallback;
		}

		private RoomProperty<T> Create<T>(string key, bool expose = false)
		{
			var property = new RoomProperty<T>(key, expose);
			InitProperty(property);
			return property;
		}

		private RoomProperty<T> CreateEnum<T>(string key, bool expose = false) where T : struct, Enum
		{
			var property = new EnumProperty<T>(key, expose);
			InitProperty(property);
			return property;
		}

		private ListRoomProperty CreateList(string key, bool expose = false)
		{
			var property = new ListRoomProperty(key, expose);
			InitProperty(property);
			return property;
		}
		
		private ListEnumRoomProperty<T> CreateEnumList<T>(string key, bool expose = false) where T : struct, Enum
		{
			var property = new ListEnumRoomProperty<T>(key, expose);
			InitProperty(property);
			return property;
		}


		private void OnLocalPlayerSetPropertyCallback(IRoomProperty property)
		{
			OnLocalPlayerSetProperty?.Invoke(property.Key, property.ToRaw());
		}

		public void OnReceivedPropertyChange(string key, object value)
		{
			var roomProperty = _allProperties.Find(e => e.Key == key);
			roomProperty?.FromRaw(value);
		}

		public string[] GetExposedPropertiesIds()
		{
			return _allProperties.Where(e => e.Expose).Select(e => e.Key).ToArray();
		}

		public Hashtable ToHashTable()
		{
			var table = new Hashtable();
			foreach (var roomProperty in _allProperties.Where(roomProperty => roomProperty.HasValue))
			{
				table.Add(roomProperty.Key, roomProperty.ToRaw());
			}

			return table;
		}
	}
}