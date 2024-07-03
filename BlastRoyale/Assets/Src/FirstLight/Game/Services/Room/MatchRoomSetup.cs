using System;
using FirstLight.Game.Configs;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Represents a match join settings
	/// This object is passed down through the network to all party players so please be careful
	/// when adding data here
	/// </summary>
	[Serializable]
	public class MatchRoomSetup
	{
		public static byte Version = 1;

		// Required at creation
		public string RoomIdentifier = "";
		public GameModeRotationConfig.PlayfabQueue PlayfabQueue;
		public SimulationMatchConfig SimulationConfig;

		public byte[] ToByteArray()
		{
			var bitStream = new BitStream(8192)
			{
				Writing = true
			};
			Serialize(bitStream);
			return bitStream.ToArray();
		}

		public static bool TryParseMatchRoomSetup(byte[] bytes, out MatchRoomSetup setup)
		{
			setup = new MatchRoomSetup();
			if (bytes == null) return false;
			var bitStream = new BitStream(bytes)
			{
				Reading = true
			};
			if (!setup.Serialize(bitStream)) return false;
			
			return true;

		}

		private bool Serialize(BitStream bitStream)
		{
			PlayfabQueue ??= new GameModeRotationConfig.PlayfabQueue();

			var serializeVersion = Version;
			bitStream.Serialize(ref serializeVersion);
			if (serializeVersion != Version)
			{
				return false;
			}

			bitStream.Serialize(ref RoomIdentifier);
			bitStream.Serialize(ref PlayfabQueue.QueueName);
			bitStream.Serialize(ref PlayfabQueue.TimeoutTimeInSeconds);
			SimulationConfig ??= new SimulationMatchConfig();
			SimulationConfig.Serialize(bitStream);
			return true;
		}

		public override string ToString() => ModelSerializer.Serialize(this).Value;
	}
}