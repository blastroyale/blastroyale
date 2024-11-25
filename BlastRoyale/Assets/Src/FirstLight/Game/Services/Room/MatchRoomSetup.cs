using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Services.RoomService;
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
		// Required at creation
		public string RoomIdentifier = "";
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
			var serializeVersion = SimulationMatchConfig.Version;
			bitStream.Serialize(ref serializeVersion);
			if (serializeVersion != SimulationMatchConfig.Version)
			{
				return false;
			}

			bitStream.Serialize(ref RoomIdentifier);
			SimulationConfig ??= new SimulationMatchConfig();
			SimulationConfig.Serialize(bitStream);
			return true;
		}

		public override string ToString() => ModelSerializer.Serialize(this).Value;
	}
}