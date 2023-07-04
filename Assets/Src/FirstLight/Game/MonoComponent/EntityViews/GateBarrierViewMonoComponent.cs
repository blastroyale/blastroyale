using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Shows visuals for the gate barrier opening or closing
	/// </summary>
	public class GateBarrierViewMonoComponent : EntityViewBase
	{
		private static readonly int _open = Animator.StringToHash("open");
		private static readonly int _openWide = Animator.StringToHash("open_wide");
		private static readonly int _openSpeed = Animator.StringToHash("open_speed");

		[SerializeField, Required] private Animator _animator;
		[SerializeField, Required] private bool _isOpenWide;
		
		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnGateStartOpening>(this, OnGateStartOpening);
		}

		private void OnGateStartOpening(EventOnGateStartOpening callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}

			StartOpening(callback.OpeningTime.AsFloat);
		}

		private  void StartOpening(float openingTime)
		{
			_animator.SetFloat(_openSpeed, 1/openingTime);
			_animator.SetBool(_isOpenWide ? _openWide : _open, true);
		}
	}
}