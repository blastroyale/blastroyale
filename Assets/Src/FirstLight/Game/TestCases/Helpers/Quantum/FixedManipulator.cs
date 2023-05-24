using System.Collections;
using FirstLight.Game.Configs;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.TestCases.Helpers
{
	public class FixedManipulator : IInputManipulator
	{
		private static int ShootingDuration = 20;

		private FPVector2 _fixedMove;
		private int _shootEveryXFrames;

		private float _speed = 0;


		private int _shootingFrameCount;

		public FixedManipulator(FPVector2 fixedMove, int shootEveryXFrames = 0, float speed= 100)
		{
			_fixedMove = fixedMove;
			_shootEveryXFrames = shootEveryXFrames;
			_speed = speed;
		}


		public void ChangeInput(CallbackPollInput callback, ref Quantum.Input input)
		{
			input.SetInput(_fixedMove, _fixedMove, ShouldShoot(), FP.FromFloat_UNSAFE(_speed));
		}

		private bool ShouldShoot()
		{
			if (_shootEveryXFrames == 0)
			{
				return false;
			}

			_shootingFrameCount++;
			if (_shootingFrameCount > _shootEveryXFrames && _shootingFrameCount <= _shootEveryXFrames + ShootingDuration)
			{
				return true;
			}

			if (_shootingFrameCount > _shootEveryXFrames + ShootingDuration)
			{
				_shootingFrameCount = 0;
			}

			return false;
		}

		public IEnumerator Start()
		{
			yield break;
		}

		public void OnAwake()
		{
		}

		public void Stop()
		{
		}
	}
}