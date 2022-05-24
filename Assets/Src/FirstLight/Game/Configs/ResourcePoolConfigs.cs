using System;
using System.Collections.Generic;
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
		public float ScaleMultiplier;
		public float ShapeModifier;
		public float MaxPoolCapacityDecreaseModifier;
		public float PoolCapacityDecreaseExponent;
		public float MaxTakeDecreaseModifier;
		public float TakeDecreaseExponent;
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