using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// This Mono component controls the stadium tv mesh rendering behaviour 
	/// </summary>
	public class StadiumTvSimulationMonoComponent : MonoBehaviour
	{
		[Serializable]
		public struct StadiumMaterial
		{
			public Material Material;
			public float ShowTime;
		}
		
		[SerializeField, Required] private Renderer _mainRenderer;
		[SerializeField, Required] private StandingsHolderView _standings;
		[SerializeField] private StadiumMaterial[] _stadiumScreenMaterials;
		
		private IGameServices _services;
		private int _materialIndex;
		private float _lastShowTime;
		private Coroutine _updateTickCoroutine;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnGameCompleted);
			//_services.TickService.SubscribeOnUpdate(UpdateTick);
		}

		private void Start()
		{
			_updateTickCoroutine = _services.CoroutineService.StartCoroutine(UpdateTick());
		}

		private void OnDestroy()
		{
			if (_updateTickCoroutine != null)
			{
				_services?.CoroutineService.StopCoroutine(_updateTickCoroutine);
				_updateTickCoroutine = null;
			}
			//_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}

		private IEnumerator UpdateTick()
		{
			if (Time.time - _lastShowTime < _stadiumScreenMaterials[_materialIndex].ShowTime)
			{
				yield return null;
			}

			_materialIndex++;

			if (_materialIndex >= _stadiumScreenMaterials.Length)
			{
				_materialIndex = 0;
			}

			_lastShowTime = Time.time;
			_mainRenderer.material = _stadiumScreenMaterials[_materialIndex].Material;
		}
		
		private void OnGameCompleted(EventOnGameEnded callback)
		{
			var playerData = callback.PlayersMatchData;
			
			_standings.Initialise(playerData.Count, true, true);
			_standings.UpdateStandings(playerData, QuantumRunner.Default.Game.GetLocalPlayers()[0]);
			//_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
			if (_updateTickCoroutine != null)
			{
				_services?.CoroutineService.StopCoroutine(_updateTickCoroutine);
				_updateTickCoroutine = null;
			}
		}
	}
}