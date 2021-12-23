using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Handles logic for the Map Timer in Battle Royale mode.
	/// </summary>
	public class MapTimerView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _mapStatusText;
		[SerializeField] private GameObject _timerHolder;
		[SerializeField] private TextMeshProUGUI _timerText;
		[SerializeField] private Animation _mapStatusTextAnimation;
		[SerializeField] private GameObject _timerOutline;
		[SerializeField] private Animation _mapShrinkingTimerAnimation;
		[SerializeField] private Transform _safeAreaRadialTransform;
		
		private IGameServices _services;
		private Transform _cameraTransform;

		public void UpdateShrinkingCircle(Frame f, ShrinkingCircle circle)
		{
			StartCoroutine(UpdateShrinkingCircleTimer(f, circle));
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);
			
			_cameraTransform = Camera.main.transform;
		}
		
		private IEnumerator UpdateShrinkingCircleTimer(Frame f, ShrinkingCircle circle)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
			var time = (circle.ShrinkingStartTime - f.Time - config.WarningTime).AsFloat;
			
			yield return new WaitForSeconds(time);

			time = Time.time + (circle.ShrinkingStartTime - QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;
			_mapStatusText.text = ScriptLocalization.AdventureMenu.GoToArea;
			
			_mapStatusText.gameObject.SetActive(true);
			_timerHolder.SetActive(true);
			
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();

			var targetPosLocal = _cameraTransform.InverseTransformPoint(circle.TargetCircleCenter.ToUnityVector3());
			var targetAngle = -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg;
			// Apply rotation
			_safeAreaRadialTransform.eulerAngles = new Vector3(0, 0, targetAngle);
			
			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");

				yield return null;
			}
			
			_mapStatusText.text = ScriptLocalization.AdventureMenu.AreaShrinking;
			time = Time.time + (circle.ShrinkingStartTime + circle.ShrinkingDurationTime - 
			                    QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;
			
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();
			
			_timerOutline.SetActive(true);
			_mapShrinkingTimerAnimation.Rewind();
			_mapShrinkingTimerAnimation.Play();

			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");

				yield return null;
			}
			
			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);
			_mapStatusText.gameObject.SetActive(false);
		}

	}
}

