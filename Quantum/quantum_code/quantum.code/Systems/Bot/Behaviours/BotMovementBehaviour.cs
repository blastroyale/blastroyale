using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotMovementBehaviour : BotBehaviour
	{
		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (CheckGrounded(f, ref filter))
			{
				return true;
			}

			CheckSpeedResetAfterLending(f, ref filter);


			return false;
		}

		private void CheckSpeedResetAfterLending(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (!filter.BotCharacter->SpeedResetAfterLanding)
			{
				filter.BotCharacter->SpeedResetAfterLanding = true;

				// We call stop aiming once here to set the movement speed to a proper stat value
				UpdateMovementSpeed(f, ref filter);
			}
		}


		public void UpdateMovementSpeed(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var speed = f.Get<Stats>(filter.Entity).Values[(int) StatType.Speed].StatValue;
			speed *= filter.BotCharacter->MovementSpeedMultiplier;

			var speedUpMutatorExists = f.Context.TryGetMutatorByType(MutatorType.Speed, out var speedUpMutatorConfig);
			speed = speedUpMutatorExists ? speed * speedUpMutatorConfig.Param1 : speed;

			// When we clear the target we also return speed to normal
			// because without a target bots don't shoot
			f.Unsafe.GetPointer<CharacterController3D>(filter.Entity)->MaxSpeed = speed;
		}

		private bool CheckGrounded(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var kcc = f.Unsafe.GetPointer<CharacterController3D>(filter.Entity);

			if (!kcc->Grounded)
			{
				kcc->Velocity.Y = -FP._0_50 - FP._0_20;
				kcc->Move(f, filter.Entity, FPVector3.Zero);

				// TODO Nik: Make a specific branching decision in case we skydive in Battle Royale
				// instead of just Move to zero direction we need to choose a target to move to, based on bot BotBehaviourType,
				// then store this target in blackboard (to not search again) and keep moving towards it

				return true;
			}

			return false;
		}
	}
}