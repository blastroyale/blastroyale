using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Server.SDK.Models;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's resources
	/// </summary>
	public interface IResourceDataProvider
	{
		/// <summary>
		/// Requests the player's resource pool data. <see cref="IObservableDictionary"/>
		/// </summary>
		IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools { get; }

		/// <summary>
		/// Requests the current <see cref="ResourcePoolInfo"/> of a given <paramref name="poolType"/>,
		/// based on various factors, such as amount of NFTs owned
		/// </summary>
		ResourcePoolInfo GetResourcePoolInfo(GameId poolType);
	}

	/// <inheritdoc />
	public interface IResourceLogic : IResourceDataProvider
	{
		/// <summary>
		/// Tries to withdraw and award a currency/resource from a given <paramref name="pool"/>
		/// </summary>
		/// <returns>Amount of currency/resource that was awarded from resource pool.</returns>
		uint WithdrawFromResourcePool(GameId pool, uint amountToAward);
	}

	/// <inheritdoc cref="IResourceLogic"/>
	public class ResourceLogic : AbstractBaseLogic<PlayerData>, IResourceLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameId, ResourcePoolData> _resourcePools;
		
		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools => _resourcePools;
		
		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

		public ResourceLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var defaultValues = new PlayerData().ResourcePools;
			
			_resourcePools = new ObservableDictionary<GameId, ResourcePoolData>(Data.ResourcePools);

			foreach (var pair in defaultValues)
			{
				if (!_resourcePools.ContainsKey(pair.Key))
				{
					_resourcePools.Add(pair.Key, pair.Value);
				}
			}
		}

		public void ReInit()
		{
			var defaultValues = new PlayerData().ResourcePools;
			
			{
				var listeners = _resourcePools.GetObservers();
				_resourcePools = new ObservableDictionary<GameId, ResourcePoolData>(Data.ResourcePools);
				_resourcePools.AddObservers(listeners);
			}
			
			foreach (var pair in defaultValues)
			{
				if (!_resourcePools.ContainsKey(pair.Key))
				{
					_resourcePools.Add(pair.Key, pair.Value);
				}
			}
			
			_resourcePools.InvokeUpdate();
		}

		/// <inheritdoc />
		public ResourcePoolInfo GetResourcePoolInfo(GameId poolType)
		{
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int) poolType);
			var capacity = GetCurrentPoolCapacity(poolType);
			var pool = _resourcePools[poolType];
			var totalRestock = poolConfig.TotalRestockIntervalMinutes / poolConfig.RestockIntervalMinutes;
			var minutesElapsedSinceLastRestock = (DateTime.UtcNow - pool.LastPoolRestockTime).TotalMinutes;
			var amountOfRestocks = (uint) Math.Floor(minutesElapsedSinceLastRestock / poolConfig.RestockIntervalMinutes);
			var restockPerInterval = (uint) Math.Floor((double) capacity / totalRestock);
			var nextRestockMinutes = (amountOfRestocks + 1) * poolConfig.RestockIntervalMinutes;
			var addAmount = amountOfRestocks * restockPerInterval;

			return new ResourcePoolInfo
			{
				Id = poolType,
				Config = poolConfig,
				PoolCapacity = capacity,
				CurrentAmount = Math.Min(pool.CurrentResourceAmountInPool + addAmount, capacity),
				WinnerRewardAmount = 0,
				RestockPerInterval = restockPerInterval,
				NextRestockTime = pool.LastPoolRestockTime.AddMinutes(nextRestockMinutes)
			};
		}

		/// <inheritdoc />
		public uint WithdrawFromResourcePool(GameId poolType, uint amountToAward)
		{
			var poolInfo = GetResourcePoolInfo(poolType);
			var poolData = _resourcePools[poolType];
			var amountWithdrawn = amountToAward > poolInfo.CurrentAmount ? poolInfo.CurrentAmount : amountToAward;
			var restockTime = poolInfo.NextRestockTime.AddMinutes(-poolInfo.Config.RestockIntervalMinutes);

			poolData.CurrentResourceAmountInPool = poolInfo.CurrentAmount - amountWithdrawn;
			poolData.LastPoolRestockTime = poolInfo.IsFull ? DateTime.UtcNow : restockTime;
			
			_resourcePools[poolType] = poolData;

			return amountWithdrawn;
		}

		private uint GetCurrentPoolCapacity(GameId poolType)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup
			//
			// This calculation can be tested by using specific equipment.
			// Get this test equipment by using SROptions.Cheats.RemoveAllEquipment, and then SROptions.Cheats.UnlockEquipmentSet
			// Test calculations for this algorithm can be found at the bottom of this spreadsheet:
			// https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=1028779545

			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);
			return poolConfig.PoolCapacity;
		}
	}
}