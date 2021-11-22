using System;
using Photon.Deterministic;

namespace Quantum
{
	public partial struct Multishot
	{
		public Multishot(uint level, QuantumMultishotConfig config) : this()
		{
			Level = level;
			BaseAmount = config.BaseAmount;
			BaseShootTimeGap = config.BaseShootTimeGap;
			AmountLevelUpStep = config.AmountLevelUpStep;
			ShootTimeGapLevelUpStep = config.ShootTimeGapLevelUpStep;
		}
	}
	public partial struct Frontshot
	{
		public Frontshot(uint level, QuantumFrontshotConfig config) : this()
		{
			Level = level;
			BaseAmount = config.BaseAmount;
			Spread = config.Spread;
			AmountLevelUpStep = config.AmountLevelUpStep;
		}
	}
	
	public partial struct Diagonalshot
	{
		public Diagonalshot(uint level, QuantumDiagonalshotConfig config) : this()
		{
			Level = level;
			BaseAmount = config.BaseAmount;
			BaseAngle = config.BaseAngle;
			AmountLevelUpStep = config.AmountLevelUpStep;
			AngleLevelUpStep = config.AngleLevelUpStep;
		}
	}
	
	public static class PowerUps
	{
		/// <summary>
		/// Adds the given Power Up <paramref name="powerUp"/> to of the given character's <paramref name="entity"/>
		/// </summary>
		public static void AddPowerUpToEntity(Frame f, EntityRef entity, GameId powerUp, GameId configGameId)
		{
			var level = GetEntityPowerUpLevel(f, entity, powerUp);
			
			if (level == 0)
			{
				CreateNewPowerUp(f, entity, powerUp, level + 1, configGameId);
			}
			else
			{
				LevelUpPowerUp(f, entity, powerUp, level + 1);
			}
		}

		/// <summary>
		/// Removes all Power Ups from the character's <paramref name="entity"/>
		/// </summary>
		public static void RemovePowerupsFromEntity(Frame f, EntityRef entity)
		{
			if (f.Has<Multishot>(entity))
			{
				f.Remove<Multishot>(entity);
			}
			if (f.Has<Diagonalshot>(entity))
			{
				f.Remove<Diagonalshot>(entity);
			}
			if (f.Has<Frontshot>(entity))
			{
				f.Remove<Frontshot>(entity);
			}
		}
		
		/// <summary>
		/// Requests the Power Up level of the given character's <paramref name="entity"/>
		/// </summary>
		public static uint GetEntityPowerUpLevel(Frame f, EntityRef entity, GameId powerUp)
		{
			if (entity == EntityRef.None)
			{
				return 0;
			}
			
			switch (powerUp)
			{
				case GameId.Multishot:
					if (f.Has<Multishot>(entity))
					{
						return f.Get<Multishot>(entity).Level;
					}
					break;
				case GameId.Diagonalshot:
					if (f.Has<Diagonalshot>(entity))
					{
						return f.Get<Diagonalshot>(entity).Level;
					}
					break;
				case GameId.Frontshot:
					if (f.Has<Frontshot>(entity))
					{
						return f.Get<Frontshot>(entity).Level;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(powerUp), powerUp, null);
			}
			
			return 0;
		}

		private static void CreateNewPowerUp(Frame f, EntityRef entity, GameId powerUp, uint level, GameId configGameId)
		{
			switch (powerUp)
			{
				case GameId.Multishot:
					f.Add(entity, new Multishot(level, f.MultishotConfigs.QuantumConfigs.Find(x => x.Id == configGameId)));
					break;
				case GameId.Diagonalshot:
					f.Add(entity, new Diagonalshot(level, f.DiagonalshotConfigs.QuantumConfigs.Find(x => x.Id == configGameId)));
					break;
				case GameId.Frontshot:
					f.Add(entity, new Frontshot(level, f.FrontshotConfigs.QuantumConfigs.Find(x => x.Id == configGameId)));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(GameId), powerUp, null);
			}
		}
		
		private static unsafe void LevelUpPowerUp(Frame f, EntityRef entity, GameId powerUp, uint level)
		{
			switch (powerUp)
			{
				case GameId.Multishot:
					f.Unsafe.GetPointer<Multishot>(entity)->Level = level;
					break;
				case GameId.Diagonalshot:
					f.Unsafe.GetPointer<Diagonalshot>(entity)->Level = level;
					break;
				case GameId.Frontshot:
					f.Unsafe.GetPointer<Frontshot>(entity)->Level = level;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(GameId), powerUp, null);
			}
		}
	}
}