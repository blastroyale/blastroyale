using System;
using System.Collections;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Handles logic for the Map Timer in Battle Royale mode.
	/// </summary>
	public class MapTimerView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _mapStatusText;
		[SerializeField, Required] private GameObject _timerHolder;
		[SerializeField, Required] private TextMeshProUGUI _timerText;
		[SerializeField, Required] private Animation _mapStatusTextAnimation;
		[SerializeField, Required] private GameObject _timerOutline;
		[SerializeField, Required] private Animation _mapShrinkingTimerAnimation;
		[SerializeField, Required] private Transform _safeAreaRadialTransform;

		private IGameServices _services;
		private Transform _cameraTransform;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_cameraTransform = Camera.main.transform;

			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle, onlyIfActiveAndEnabled: true);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.Unsubscribe<MatchStartedMessage>(OnMatchStarted);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;

			if (frame.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				StartCoroutine(UpdateShrinkingCircleTimer(frame));
			}
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			StartCoroutine(UpdateShrinkingCircleTimer(callback.Game.Frames.Verified));
		}

		private IEnumerator UpdateShrinkingCircleTimer(Frame f)
		{
			var circle = f.GetSingleton<ShrinkingCircle>();
			var config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
			var time = (circle.ShrinkingStartTime - f.Time - config.WarningTime).AsFloat;
			var targetCircleCenter = circle.TargetCircleCenter.ToUnityVector3();
			var circleRadius = circle.TargetRadius.AsFloat;

			yield return new WaitForSeconds(time);
 

			time = Time.time + (circle.ShrinkingStartTime - QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;
			_mapStatusText.text = ScriptLocalization.AdventureMenu.GoToArea;

			_mapStatusText.gameObject.SetActive(true);
			_timerHolder.SetActive(true);
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();

			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");

				UpdateDirectionPointer(targetCircleCenter, circleRadius);

				yield return null;
			}

			_mapStatusText.text = ScriptLocalization.AdventureMenu.AreaShrinking;
			time = Time.time + (circle.ShrinkingStartTime + circle.ShrinkingDurationTime -
			                    QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;

			_timerOutline.SetActive(true);
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();
			_mapShrinkingTimerAnimation.Rewind();
			_mapShrinkingTimerAnimation.Play();

			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");
				
				UpdateDirectionPointer(targetCircleCenter, circleRadius);

				yield return null;
			}

			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);
			_mapStatusText.gameObject.SetActive(false);
		}

		private void UpdateDirectionPointer(Vector3 targetCircleCenter, float circleRadius)
		{
			// Calculate and Apply rotation
			var targetPosLocal = _cameraTransform.InverseTransformPoint(targetCircleCenter);
			var targetAngle = -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg;
			var isArrowActive = _safeAreaRadialTransform.gameObject.activeSelf;
			var circleRadiusSq = circleRadius * circleRadius;
			var distanceSqrt = (targetCircleCenter - _cameraTransform.position).sqrMagnitude;

			_safeAreaRadialTransform.eulerAngles = new Vector3(0, 0, targetAngle);

			if (distanceSqrt < circleRadiusSq && isArrowActive)
			{
				_safeAreaRadialTransform.gameObject.SetActive(false);
			}
			else if (distanceSqrt > circleRadiusSq && !isArrowActive)
			{
				_safeAreaRadialTransform.gameObject.SetActive(true);
			}
		}
	}
}


