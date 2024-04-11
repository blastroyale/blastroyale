using Quantum;
using Sirenix.OdinInspector;
using SRF;
using UnityEngine;
using LayerMask = UnityEngine.LayerMask;

namespace FirstLight.Game.MonoComponent.Collections
{
	public class CharacterSkinMonoComponent : MonoBehaviour, ISelfValidator
	{
		// ReSharper disable InconsistentNaming
		private readonly int P_MOVING = Animator.StringToHash("moving");
		private readonly int P_AIMING = Animator.StringToHash("aiming");

		private readonly int P_ATTACK = Animator.StringToHash("attack");
		private readonly int P_VICTORY = Animator.StringToHash("victory");
		private readonly int P_HIT = Animator.StringToHash("hit");
		private readonly int P_DIE = Animator.StringToHash("die");
		private readonly int P_SKYDIVE = Animator.StringToHash("skydive");
		private readonly int P_PLF = Animator.StringToHash("plf");
		private readonly int P_FLAIR = Animator.StringToHash("flair");
		private readonly int P_KNOCK_OUT = Animator.StringToHash("knock_out");
		private readonly int P_STUN = Animator.StringToHash("stun");
		private readonly int P_RESTORE = Animator.StringToHash("restore");

		private readonly int P_WEAPON_TYPE = Animator.StringToHash("weapon_type");
		private readonly int P_WEAPON_TYPE_FLOAT = Animator.StringToHash("weapon_type_float");
		// ReSharper restore InconsistentNaming

		[SerializeField] private Transform _weaponAnchor;
		[SerializeField] private Transform _weaponXLMeleeAnchor;
		[SerializeField] private Transform _gliderAnchor;
		[SerializeField] private Transform _leftFootAnchor;
		[SerializeField] private Transform _rightFootAnchor;
		[SerializeField] private Animator _animator;
		[SerializeField] private CharacterSkinEventsMonoComponent _events;

		public Transform WeaponAnchor => _weaponAnchor;
		public Transform WeaponXLMeleeAnchor => _weaponXLMeleeAnchor;
		public Transform GliderAnchor => _gliderAnchor;
		public Transform LeftFootAnchor => _leftFootAnchor;
		public Transform RightFootAnchor => _rightFootAnchor;

		public CharacterSkinEventsMonoComponent Events => _events;

		public bool Moving
		{
			set => _animator.SetBool(P_MOVING, value);
		}

		public bool Aiming
		{
			set => _animator.SetBool(P_AIMING, value);
		}

		public bool Meta
		{
			set => _animator.SetLayerWeight(_animator.GetLayerIndex("Meta"), value ? 1f : 0f);
		}

		public WeaponType WeaponType
		{
			set
			{
				_animator.SetInteger(P_WEAPON_TYPE, (int) value);
				_animator.SetFloat(P_WEAPON_TYPE_FLOAT, (float) value);
			}
		}

		public void TriggerHit() => _animator.SetTrigger(P_HIT);
		public void TriggerDie() => _animator.SetTrigger(P_DIE);
		public void TriggerRestore() => _animator.SetTrigger(P_RESTORE);
		public void TriggerStun() => _animator.SetTrigger(P_STUN);
		public void TriggerAttack() => _animator.SetTrigger(P_ATTACK);
		public void TriggerVictory() => _animator.SetTrigger(P_VICTORY);
		public void TriggerPLF() => _animator.SetTrigger(P_PLF);
		public void TriggerSkydive() => _animator.SetTrigger(P_SKYDIVE);
		public void TriggerKnockOut() => _animator.SetTrigger(P_KNOCK_OUT);
		public void TriggerFlair() => _animator.SetTrigger(P_FLAIR);

		public void Validate(SelfValidationResult result)
		{
#if UNITY_EDITOR
			// Anchors and components
			if (_weaponAnchor == null)
				result.AddError("Missing weapon anchor!").WithFix(() => _weaponAnchor = transform.Find($"{name}/weapon"));
			if (_weaponXLMeleeAnchor == null)
				result.AddError("Missing weapon XL anchor!").WithFix(() => _weaponXLMeleeAnchor = transform.Find($"{name}/weapon_xlmelee"));
			if (_gliderAnchor == null)
				result.AddError("Missing glider anchor!").WithFix(() => _gliderAnchor = transform.Find($"{name}/glider"));
			if (_leftFootAnchor == null)
				result.AddError("Missing left foot anchor!").WithFix(() => _leftFootAnchor = transform.Find($"{name}/Foot.L"));
			if (_rightFootAnchor == null)
				result.AddError("Missing right foot anchor!").WithFix(() => _rightFootAnchor = transform.Find($"{name}/Foot.R"));
			if (_animator == null)
			{
				result.AddError("Missing animator!").WithFix(() => _animator = gameObject.GetComponentInChildren<Animator>());
			}
			else
			{
				if (_animator.applyRootMotion)
					result.AddError("Animator should not apply root motion!").WithFix(() => _animator.applyRootMotion = false);
				if (_animator.runtimeAnimatorController == null)
					result.AddError("Missing animator controller!").WithFix(() =>
						_animator.runtimeAnimatorController =
							UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
								"Assets/AddressableResources/Collections/CharacterSkins/Shared/character_animator.controller"));
			}

			if (_events == null)
				result.AddError("Missing events component!")
					.WithFix(() => _events = gameObject.GetComponentInChildren<CharacterSkinEventsMonoComponent>());

			// Layer TODO: Does not check if all children are on the right layer
			var playersLayer = LayerMask.NameToLayer("Players");
			if (gameObject.layer != playersLayer)
				result.AddError("Character skin should be on the Players layer!").WithFix(() => gameObject.SetLayerRecursive(playersLayer));
#endif
		}
	}
}