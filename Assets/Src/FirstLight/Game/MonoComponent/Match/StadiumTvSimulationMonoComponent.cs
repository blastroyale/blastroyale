using System;
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
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnGameCompleted);
			_services.TickService.SubscribeOnUpdate(UpdateTick);
		}

		private void OnDestroy()
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}

		private void UpdateTick(float deltaTime)
		{
			if (Time.time - _lastShowTime < _stadiumScreenMaterials[_materialIndex].ShowTime)
			{
				return;
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
			_standings.UpdateStandings(playerData, callback.Game.GetLocalPlayers()[0]);
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}
	}
}