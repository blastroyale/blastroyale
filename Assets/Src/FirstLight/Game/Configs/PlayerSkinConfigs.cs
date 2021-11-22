using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct PlayerSkinConfig
	{
		public GameId Id;
		public uint Price;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="PlayerSkinConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "PlayerSkinConfigs", menuName = "ScriptableObjects/Configs/PlayerSkinConfigs")]
	public class PlayerSkinConfigs : ScriptableObject, IConfigsContainer<PlayerSkinConfig>
	{
		[SerializeField] private List<PlayerSkinConfig> _configs = new List<PlayerSkinConfig>();

		// ReSharper disable once ConvertToAutoProperty
		public List<PlayerSkinConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}