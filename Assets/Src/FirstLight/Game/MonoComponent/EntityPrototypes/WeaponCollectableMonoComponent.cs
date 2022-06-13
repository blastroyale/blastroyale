using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="WeaponCollectable"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class WeaponCollectableMonoComponent : EntityBase
	{
		[SerializeField, Required] private Transform _itemTransform;
		[SerializeField, Required] private CollectableViewMonoComponent _collectableView;

		[SerializeField, Required] private GameObject _debugContainer;
		[SerializeField, Required] private TextMeshProUGUI _debugText;

		protected override async void OnEntityInstantiated(QuantumGame game)
		{
			_collectableView.SetEntityView(game, EntityView);

			var collectable = GetComponentData<Collectable>(game);
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(collectable.GameId);
			var cacheTransform = instance.transform;

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return;
			}

			cacheTransform.SetParent(_itemTransform);

			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localScale = Vector3.one;
			cacheTransform.localRotation = Quaternion.identity;

			if (SROptions.Current.EnableEquipmentDebug)
			{
				_debugContainer.SetActive(true);
				var equipmentCollectable = GetComponentData<EquipmentCollectable>(game);

				_debugText.text = equipmentCollectable.Item.Rarity.ToString();
				if (equipmentCollectable.Owner != PlayerRef.None)
				{
					_debugText.text += $"\nOwner: {equipmentCollectable.Owner}";
				}
			}
			else
			{
				Destroy(_debugContainer);
			}
		}
	}
}