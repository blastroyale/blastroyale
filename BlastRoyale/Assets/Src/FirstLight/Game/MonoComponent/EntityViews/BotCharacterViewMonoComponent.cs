using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Specific rendering behaviour for bots.d
	/// Bots do not rotate towards their aim inside the simulation
	/// because they always walk towards what they are looking at.
	///
	/// This view rotates bots towards where they are aiming at to cover that.
	/// </summary>
	public class BotCharacterViewMonoComponent : EntityViewBase
	{
		unsafe void LateUpdate()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			if (!f.Unsafe.TryGetPointer<BotCharacter>(EntityRef, out var bot)) return;
			if (!f.Unsafe.TryGetPointer<AIBlackboardComponent>(EntityRef, out var bb)) return;
			var aiming = bb->GetBoolean(f, Constants.IsAimPressedKey);
			if (bot->Target.IsValid && aiming)
			{
				transform.rotation = Quaternion.LookRotation(bb->GetVector2(f, Constants.AimDirectionKey).ToUnityVector3());
			}
			else if(transform.localRotation != Quaternion.identity)
			{
				transform.localRotation = Quaternion.identity;
			}
		}
	}
}