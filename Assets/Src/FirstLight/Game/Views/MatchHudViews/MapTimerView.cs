using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
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
			
			var targetCircleCenter = circle.TargetCircleCenter.ToUnityVector3();
			
			time = Time.time + (circle.ShrinkingStartTime - QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;
			_mapStatusText.text = ScriptLocalization.AdventureMenu.GoToArea;
			
			_mapStatusText.gameObject.SetActive(true);
			_timerHolder.SetActive(true);
			
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();
			
			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");
				
				UpdateDirectionPointer(targetCircleCenter);
				
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
				
				UpdateDirectionPointer(targetCircleCenter);
				
				yield return null;
			}
			
			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);
			_mapStatusText.gameObject.SetActive(false);
		}

		private void UpdateDirectionPointer(Vector3 targetCircleCenter)
		{
			// Calculate and Apply rotation
			var targetPosLocal = _cameraTransform.InverseTransformPoint(targetCircleCenter);
			var targetAngle = -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg;
			_safeAreaRadialTransform.eulerAngles = new Vector3(0, 0, targetAngle);
		}

	}
}

