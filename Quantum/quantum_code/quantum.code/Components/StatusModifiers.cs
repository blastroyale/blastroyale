using System;
using Photon.Deterministic;

namespace Quantum
{
	public static class StatusModifiers
	{
		/// <summary>
		/// Adds the given Status Modifier <paramref name="statusModifierType"/> to the given character's <paramref name="entity"/>
		/// </summary>
		public static void AddStatusModifierToEntity(Frame f, EntityRef entity, StatusModifierType statusModifierType, FP duration)
		{
			AddStatusModifierToEntity(f, entity, statusModifierType, duration, false);
		}

		/// <summary>
		/// Adds the given Status Modifier <paramref name="statusModifierType"/> to the given character's <paramref name="entity"/>
		/// </summary>
		public static void AddStatusModifierToEntity(Frame f, EntityRef entity, StatusModifierType statusModifierType, FP duration, bool isLogicSideOnly)
		{
			if (!CancelCurrentStatusModifier(f, entity))
			{
				return;
			}
			
			CreateNewStatusModifier(f, entity, statusModifierType, duration);
			
			// Do not send event to Unity side if we only need the logic of the status and not the visual side
			if (isLogicSideOnly)
			{
				return;
			}
			
			f.Events.OnStatusModifierSet(entity, statusModifierType, duration);
		}

		/// <summary>
		/// Finishes the current Status Modifier of the given <paramref name="entity"/>
		/// </summary>
		public static unsafe void FinishCurrentStatusModifier(Frame f, EntityRef entity)
		{
			if (!f.TryGet<Stats>(entity, out var stats))
			{
				throw new AssertException(entity + " has no Stats to remove the current StatusModifier from");
			}
			
			switch (stats.CurrentStatusModifierType)
			{
				case StatusModifierType.Invisibility:
					f.Remove<Invisibility>(entity);
					break;
				case StatusModifierType.Rage:
					f.Remove<Rage>(entity);
					break;
				case StatusModifierType.Regeneration:
					f.Remove<Regeneration>(entity);
					break;
				case StatusModifierType.Stun:
					f.Remove<Stun>(entity);
					break;
				case StatusModifierType.Shield:
					f.Remove<Shield>(entity);
					break;
				case StatusModifierType.Star:
					f.Remove<Star>(entity);
					break;
			}
			
			f.Unsafe.GetPointer<Stats>(entity)->CurrentStatusModifierType = StatusModifierType.None;
			
			f.Events.OnStatusModifierFinished(entity, stats.CurrentStatusModifierType);
		}

		private static bool CancelCurrentStatusModifier(Frame f, EntityRef entity)
		{
			if (!f.TryGet<Stats>(entity, out var stats))
			{
				throw new AssertException(entity + " has no Stats to get StatusModifierType from");
			}

			if (stats.CurrentStatusModifierType == StatusModifierType.None)
			{
				return true;
			}
			
			// If entity IsImmune then we can't cancel anything
			if (stats.IsImmune)
			{
				return false;
			}

			switch (stats.CurrentStatusModifierType)
			{
				case StatusModifierType.Invisibility:
					f.Remove<Invisibility>(entity);
					break;
				case StatusModifierType.Rage:
					f.Remove<Rage>(entity);
					break;
				case StatusModifierType.Regeneration:
					f.Remove<Regeneration>(entity);
					break;
				case StatusModifierType.Stun:
					f.Remove<Stun>(entity);
					break;
			}
			
			stats.CurrentStatusModifierType = StatusModifierType.None;
			
			f.Signals.StatusModifierCancelled(entity, stats.CurrentStatusModifierType);
			f.Events.OnStatusModifierCancelled(entity, stats.CurrentStatusModifierType);
			
			return true;
		}

		private static unsafe void CreateNewStatusModifier(Frame f, EntityRef entity, StatusModifierType statusModifierType, FP duration)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(entity, out var stats))
			{
				throw new AssertException(entity + " has no Stats to set StatusModifierType to");
			}
			
			// We change stats first and then add an actual component
			// because we need the data from stats when we handle component added
			stats->CurrentStatusModifierType = statusModifierType;
			stats->CurrentStatusModifierDuration = duration;
			stats->CurrentStatusModifierEndTime = f.Time + duration;
			
			switch (statusModifierType)
			{
				case StatusModifierType.Shield:
					f.Add<Shield>(entity);
					break;
				case StatusModifierType.Star:
					var starComponent = new Star();
					starComponent.Power = duration;
					f.Add(entity, starComponent);
					break;
				case StatusModifierType.Invisibility:
					f.Add<Invisibility>(entity);
					break;
				case StatusModifierType.Rage:
					var rageComponent = new Rage();
					rageComponent.Power = f.GameConfig.RageStatusDamageMultiplier;
					f.Add(entity, rageComponent);
					break;
				case StatusModifierType.Regeneration:
					// TODO
					break;
				case StatusModifierType.Stun:
					var stunComponent = new Stun();
					f.Add(entity, stunComponent);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(statusModifierType), statusModifierType, null);
			}
		}
	}
}