using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct CurrencyPoolConfig
	{
		public GameId CurrencyID;
		public uint PoolCapacity;
		public uint RestockIntervalMinutes;
		public uint TotalRestockIntervalMinutes;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="RarityConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "CurrencyPoolConfigs", menuName = "ScriptableObjects/Configs/CurrencyPoolConfigs")]
	public class CurrencyPoolConfigs : ScriptableObject, IConfigsContainer<CurrencyPoolConfig>
	{
		[SerializeField] private List<CurrencyPoolConfig> _configs = new List<CurrencyPoolConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<CurrencyPoolConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}