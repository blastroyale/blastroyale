using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Match
{
	public interface IWeaponCustomizationService
	{
		
	}
	
	public class WeaponCustomizationService : IWeaponCustomizationService, MatchServices.IMatchService
	{
		private IGameServices _services;
		private Material _goldenMaterial;
		
		public WeaponCustomizationService(IGameServices services)
		{
			_services = services;
			_services.MessageBrokerService.Subscribe<EquipmentInstantiatedMessage>(OnEquipmentInstantiate);
		}

		private void OnEquipmentInstantiate(EquipmentInstantiatedMessage msg)
		{
			if (msg.Equipment.Material == EquipmentMaterial.Golden)
			{
				_ = SetGolden(msg.Object);
			}
		}

		private async UniTaskVoid SetGolden(GameObject o)
		{
			if (_goldenMaterial == null)
			{
				_goldenMaterial = await _services.AssetResolverService.RequestAsset<MaterialVfxId, Material>(MaterialVfxId.Golden);
			}

			if (o == null) return;
			
			var renderers = o.GetComponentsInChildren<MeshRenderer>();
			foreach (var rend in renderers)
			{
				rend.material = _goldenMaterial;
			}
			var gunRenderer = o.GetComponentInChildren<RenderersContainerMonoComponent>();
			var vfx = MainInstaller.ResolveServices().VfxService.Spawn(VfxId.GoldenEffect);
			vfx.transform.SetParent(gunRenderer.transform, false);
			gunRenderer.UpdateRenderers();
		}

		public void Dispose()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			Dispose();
		}
	}
}