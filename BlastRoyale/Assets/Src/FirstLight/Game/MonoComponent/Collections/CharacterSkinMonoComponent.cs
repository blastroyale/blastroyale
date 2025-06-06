﻿using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Quantum;
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
		private readonly int P_EQUIP_MELEE = Animator.StringToHash("equip_melee");
		private readonly int P_EQUIP_GUN = Animator.StringToHash("equip_gun");
		private readonly int P_ENTER = Animator.StringToHash("enter");

		private readonly int P_WEAPON_TYPE = Animator.StringToHash("weapon_type");
		private readonly int P_WEAPON_TYPE_FLOAT = Animator.StringToHash("weapon_type_float");
		// ReSharper restore InconsistentNaming

		[SerializeField] private Transform _weaponAnchor;
		[SerializeField] private Transform _weaponMeleeAnchor;
		[SerializeField] private Transform _weaponXLMeleeAnchor;
		[SerializeField] private Transform _gliderAnchor;
		[SerializeField] private Transform _leftFootAnchor;
		[SerializeField] private Transform _rightFootAnchor;
		[SerializeField] private Animator _animator;

		public event Action OnTriggerFlair;
		private static RuntimeAnimatorController _animatorController;

		public Transform WeaponAnchor => _weaponAnchor;
		public Transform WeaponMeleeAnchor => _weaponMeleeAnchor;
		public Transform WeaponXLMeleeAnchor => _weaponXLMeleeAnchor;
		public Transform GliderAnchor => _gliderAnchor;
		public Transform LeftFootAnchor => _leftFootAnchor;
		public Transform RightFootAnchor => _rightFootAnchor;

		/// <summary>
		/// When the left footsteps / is on the ground.
		/// </summary>
		public event Action OnStepLeft;

		/// <summary>
		/// When the right footsteps / is on the ground.
		/// </summary>
		public event Action OnStepRight;

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

		public bool AnimationEnabled
		{
			set => _animator.enabled = value;
		}

		public WeaponType WeaponType
		{
			set
			{
				_animator.SetInteger(P_WEAPON_TYPE, (int) value);
				_animator.SetFloat(P_WEAPON_TYPE_FLOAT, (float) value);
			}

			get => (WeaponType) _animator.GetInteger(P_WEAPON_TYPE);
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
		public void TriggerEquipMelee() => _animator.SetTrigger(P_EQUIP_MELEE);
		public void TriggerEquipGun() => _animator.SetTrigger(P_EQUIP_GUN);
		public void TriggerEnter() => _animator.SetTrigger(P_ENTER);

		public void TriggerFlair()
		{
			_animator.SetTrigger(P_FLAIR);
			OnTriggerFlair?.Invoke();
		}

		#region AnimationClipEvents

		[UsedImplicitly]
		private void StepLeft()
		{
			OnStepLeft?.Invoke();
		}

		[UsedImplicitly]
		private void StepRight()
		{
			OnStepRight?.Invoke();
		}

		#endregion

		[Conditional("UNITY_EDITOR")]
		public void SetupReferences()
		{
			_weaponAnchor = transform.Find("weapon");
			_weaponXLMeleeAnchor = transform.Find("weapon_xlmelee");
			_weaponMeleeAnchor = transform.Find("weapon_melee");
			_gliderAnchor = transform.Find("glider");
			_leftFootAnchor = transform.Find("Foot.L");
			_rightFootAnchor = transform.Find("Foot.R");
			_animator = GetComponent<Animator>();
		}
	}
}