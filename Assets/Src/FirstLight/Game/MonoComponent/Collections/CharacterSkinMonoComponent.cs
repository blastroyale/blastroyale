using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Collections
{
	public class CharacterSkinMonoComponent : MonoBehaviour
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

		[SerializeField, Required] private Transform _weaponAnchor;
		[SerializeField, Required] private Transform _gliderAnchor;
		[SerializeField, Required] private Transform _leftFootAnchor;
		[SerializeField, Required] private Transform _rightFootAnchor;
		[SerializeField, Required] private Animator _animator;
		[SerializeField, Required] private CharacterSkinEventsMonoComponent _events;

		public Transform WeaponAnchor => _weaponAnchor;
		public Transform GliderAnchor => _gliderAnchor;
		public Transform LeftFootAnchor => _leftFootAnchor;
		public Transform RightFootAnchor => _rightFootAnchor;

		public CharacterSkinEventsMonoComponent Events => _events;

		private void Start()
		{
			// TODO mihak: TEMPORARY!!!
			_weaponAnchor.localScale = Vector3.one;
			_gliderAnchor.localScale = Vector3.one;
		}

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
	}
}