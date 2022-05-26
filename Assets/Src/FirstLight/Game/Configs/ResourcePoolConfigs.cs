using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct ResourcePoolConfig
	{
		public GameId Id;
		public uint PoolCapacity;
		public uint RestockIntervalMinutes;
		public uint TotalRestockIntervalMinutes;
		public uint BaseMaxTake;
		public FP ScaleMultiplier;
		public FP ShapeModifier;
		public FP MaxPoolCapacityDecreaseModifier;
		public FP PoolCapacityDecreaseExponent;
		public FP MaxTakeDecreaseModifier;
		public FP TakeDecreaseExponent;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="ResourcePoolConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "ResourcePoolConfigs", menuName = "ScriptableObjects/Configs/ResourcePoolConfigs")]
	public class ResourcePoolConfigs : ScriptableObject, IConfigsContainer<ResourcePoolConfig>
	{
		[SerializeField] private List<ResourcePoolConfig> _configs = new List<ResourcePoolConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<ResourcePoolConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}